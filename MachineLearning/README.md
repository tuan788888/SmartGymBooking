# Workout recommendation

Python trains offline; ASP.NET runs the exported ONNX pipeline in process. No Python server is required at runtime.

## Train

Use Python 3.12 and install the tested dependencies:

```powershell
python -m pip install -r MachineLearning/training/requirements.txt
python MachineLearning/training/train_model.py
```

On the current workstation, a local portable Python is available in `.venv`:

```powershell
.venv/python.exe MachineLearning/training/train_model.py
```

The script evaluates a deterministic 25% holdout, then fits all 20 rows for deployment. It exports preprocessing and the classifier together and checks sklearn/ONNX label parity before writing `models/workout_model.onnx`. Commit the model after retraining; build and publish copy it automatically.

## Contract

Each input has shape `[batch, 1]`. Gender, FitnessGoal, ExperienceLevel and ActivityLevel are uppercase strings; Age, BMI, SessionsPerWeek and SessionMinutes are float32. BMI uses kilograms and height in meters (customer height is stored in centimeters). The string prediction is `output_label`; probabilities are not used by the application.

`IWorkoutRecommendationService.PredictPlanTypeAsync(Customer)` returns a PlanType. The singleton owns one reusable ONNX session and DI disposes it at shutdown. Missing birth date, nonpositive height/weight, incomplete training profile and invalid scheduling preferences raise validation errors. Missing preferences default to 3 sessions and 60 minutes. Missing gender becomes OTHER, handled as an unseen category by the encoder.

Customers can POST `/Workout/Generate` from the workout list. The controller uses actual ONNX prediction, then the scoped generator selects active exercises by experience level and daily muscle groups. Plans store Title, Goal, ExperienceLevel, SessionsPerWeek, CreatedByAi and model metadata. Reps is an integer in the existing schema: a target is stored in Reps and the suggested range in Note. OrderNumber is unique within each day. No schema migration is needed.

The plan and exercise graph is saved atomically in a serializable transaction. Only one AI plan per customer per calendar day (application server local time) is allowed, including concurrent submissions. Manual plans do not consume this limit. An empty active exercise library produces a user-visible error without saving a plan. Details retain ownership checks and existing exercise video links.

## Evaluation limits

This is a supervised classification prototype with synthetic demonstration data. Several labels have only one row, so the holdout is not stratified and may contain labels absent from training. Reported accuracy is not evidence of real-world quality. Expand the dataset (initially 100–300 representative rows), add enough examples per class, and evaluate with cross-validation and independent real data before making quality claims.
