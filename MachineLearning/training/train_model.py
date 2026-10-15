"""Train offline, export preprocessing + classifier, and verify ONNX parity."""
from pathlib import Path

import numpy as np
import onnxruntime as ort
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, classification_report
from sklearn.model_selection import train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder, StandardScaler
from skl2onnx import convert_sklearn
from skl2onnx.common.data_types import FloatTensorType, StringTensorType

BASE_DIR = Path(__file__).resolve().parent.parent
CATEGORICAL = ["Gender", "FitnessGoal", "ExperienceLevel", "ActivityLevel"]
NUMERIC = ["Age", "BMI", "SessionsPerWeek", "SessionMinutes"]
FEATURES = ["Gender", "Age", "BMI", "FitnessGoal", "ExperienceLevel",
            "ActivityLevel", "SessionsPerWeek", "SessionMinutes"]


def main():
    df = pd.read_csv(BASE_DIR / "data" / "workout_training.csv")
    if df.empty or df[FEATURES + ["PlanType"]].isna().any().any():
        raise ValueError("Training data must contain complete features and labels.")
    for column in CATEGORICAL + ["PlanType"]:
        df[column] = df[column].str.strip().str.upper()
        if df[column].eq("").any():
            raise ValueError(f"Empty category in {column}")
    df[NUMERIC] = df[NUMERIC].astype(np.float32)
    if not np.isfinite(df[NUMERIC].to_numpy()).all() or (df[NUMERIC] <= 0).any().any():
        raise ValueError("Numeric features must be finite and positive.")
    pipeline = Pipeline([
        ("preprocess", ColumnTransformer([
            ("categorical", OneHotEncoder(handle_unknown="ignore", sparse_output=False), CATEGORICAL),
            ("numeric", StandardScaler(), NUMERIC),
        ])),
        ("classifier", RandomForestClassifier(n_estimators=100, random_state=42)),
    ])
    # Some demo classes have only one row, so stratification is not possible.
    x_train, x_test, y_train, y_test = train_test_split(
        df[FEATURES], df["PlanType"], test_size=0.25, random_state=42)
    pipeline.fit(x_train, y_train)
    predictions = pipeline.predict(x_test)
    print("MODEL EVALUATION")
    print(f"Accuracy: {accuracy_score(y_test, predictions):.4f}")
    print(classification_report(y_test, predictions, zero_division=0))
    print("Demo holdout only; tiny dataset and singleton classes limit this evaluation.")
    # Fit the deployment model on all available rows after holdout evaluation.
    pipeline.fit(df[FEATURES], df["PlanType"])
    initial_types = [(name, StringTensorType([None, 1]) if name in CATEGORICAL
                      else FloatTensorType([None, 1])) for name in FEATURES]
    model = convert_sklearn(pipeline, initial_types=initial_types,
                            final_types=[("output_label", StringTensorType([None])),
                                         ("probabilities", FloatTensorType([None, df["PlanType"].nunique()]))],
                            options={id(pipeline.named_steps["classifier"]): {"zipmap": False}},
                            target_opset=17)
    session = ort.InferenceSession(model.SerializeToString(), providers=["CPUExecutionProvider"])
    inputs = {name: df[[name]].to_numpy(dtype=object if name in CATEGORICAL else np.float32)
              for name in FEATURES}
    labels = session.run(["output_label"], inputs)[0]
    np.testing.assert_array_equal(labels, pipeline.predict(df[FEATURES]))
    path = BASE_DIR / "models" / "workout_model.onnx"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(model.SerializeToString())
    print(f"Exported {path}; ONNX predictions match sklearn on all {len(df)} rows.")


if __name__ == "__main__":
    main()
