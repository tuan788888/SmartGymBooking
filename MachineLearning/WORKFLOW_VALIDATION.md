# B30C workflow verification

Verified on 2026-10-15 against the local SQL Express database, through the running ASP.NET application's HTTP forms and rendered Razor pages.

- `dotnet build --no-restore`: passed, zero errors. Existing NU1902/NU1903 warnings remain for System.Security.Cryptography.Pkcs 9.0.11.
- Signed in with the supplied customer test account and opened `/Workout`: generation form rendered.
- POST without an antiforgery token: rejected with HTTP 400.
- Submitted two generation requests concurrently with valid authentication and antiforgery: exactly one redirected to the new plan's Details page.
- Rendered Details contained the prediction, success message and exercise prescription notes.
- A subsequent submission returned to the list with the daily-limit message.
- SQL verification found exactly one AI plan for the test customer that day: plan 2, `HYPERTROPHY_BEGINNER_4D`, 4 days and 20 WorkoutExercises.
- Verified Title, RandomForest/1.0 metadata, SessionsPerWeek, active exercises, unique consecutive OrderNumber within each day, no duplicate exercise within a day, and beginner muscle-gain prescription (3 sets, 10 target reps, 8–12 range in Note, 90 seconds rest).
- Direct generator calls rejected an unsupported PlanType and a second AI plan for the same customer/day.

No database schema changes or test-profile updates were needed. The generated plan remains available to the supplied test customer. This verifies the HTTP form → ONNX → generator → SQL → rendered Details flow; no interactive browser automation was used.

## Repeat manually

1. Ensure a test customer has birth date, positive height/weight, fitness goal, experience and activity level. Ensure active exercises exist.
2. Start the application, sign in as that customer and open `/Workout`.
3. On a day without an AI plan for that customer, click **Tạo giáo án bằng AI**. Confirm Details shows the generated days, exercises, sets, reps, notes and rest times. Video links appear for exercises with VideoUrl.
4. Return to the list and submit again: the daily limit must prevent another plan. Manual plans should not consume this limit.
5. Confirm the saved plan and exercises in SQL; schema fields are Title (not Name), integer Reps and required OrderNumber.
6. Check that another customer cannot open the plan's Details URL, and anonymous users are redirected to login.
