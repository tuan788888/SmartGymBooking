using System.Text;
using SmartGymBooking.Models;

namespace SmartGymBooking.Services
{
    public class DemoAIChatService : IAIChatService
    {
        public Task<string> GetResponseAsync(
            Customer customer,
            string message,
            List<Aimessage> history,
            WorkoutPlan? workoutPlan)
        {
            var question =
                message.Trim().ToLowerInvariant();

            string response;

            if (ContainsAny(
                question,
                "xin chào",
                "chào",
                "hello",
                "hi"))
            {
                response =
                    $"Xin chào {customer.FullName}. " +
                    "Tôi là AI Virtual PT của SmartGym. " +
                    "Bạn có thể hỏi tôi về giáo án, BMI, " +
                    "tăng cơ, giảm mỡ hoặc kỹ thuật tập luyện.";
            }

            else if (ContainsAny(
                question,
                "bmi",
                "chiều cao",
                "cân nặng"))
            {
                response =
                    BuildBmiResponse(customer);
            }

            else if (ContainsAny(
                question,
                "giáo án",
                "workout",
                "plan",
                "lịch tập"))
            {
                response =
                    BuildWorkoutResponse(
                        workoutPlan
                    );
            }

            else if (ContainsAny(
                question,
                "mục tiêu",
                "goal"))
            {
                response =
                    BuildGoalResponse(customer);
            }

            else if (ContainsAny(
                question,
                "tăng cơ",
                "muscle",
                "hypertrophy"))
            {
                var sessions =
                    customer.PreferredSessionsPerWeek
                    ?? 3;

                response =
                    $"Với mục tiêu tăng cơ và khoảng " +
                    $"{sessions} buổi/tuần, bạn nên ưu tiên " +
                    "tập sức mạnh, progressive overload và " +
                    "phục hồi đầy đủ. Khoảng 8–12 reps thường " +
                    "phù hợp cho nhiều bài tập tăng cơ.";
            }

            else if (ContainsAny(
                question,
                "giảm cân",
                "giảm mỡ",
                "weight loss"))
            {
                response =
                    """
                    Với mục tiêu giảm mỡ, nên kết hợp:

                    • Tập sức mạnh
                    • Cardio phù hợp
                    • Hoạt động hàng ngày
                    • Kiểm soát dinh dưỡng
                    • Ngủ và phục hồi đầy đủ

                    Không nên chỉ tập cardio và bỏ hoàn toàn tập sức mạnh.
                    """;
            }

            else if (ContainsAny(
                question,
                "ngực",
                "chest",
                "bench"))
            {
                response =
                    """
                    Một buổi tập ngực có thể gồm:

                    • Bench Press
                    • Incline Dumbbell Press
                    • Chest Press
                    • Cable Fly

                    Với người mới, nên ưu tiên kỹ thuật đúng trước khi tăng mức tạ.
                    """;
            }

            else if (ContainsAny(
                question,
                "lưng",
                "back",
                "row"))
            {
                response =
                    """
                    Một số bài tập lưng phổ biến:

                    • Lat Pulldown
                    • Seated Cable Row
                    • Dumbbell Row
                    • Face Pull

                    Hãy kiểm soát chuyển động và tập trung sử dụng cơ lưng.
                    """;
            }

            else if (ContainsAny(
                question,
                "chân",
                "leg",
                "squat"))
            {
                response =
                    """
                    Buổi tập chân cơ bản có thể gồm:

                    • Squat
                    • Leg Press
                    • Leg Curl
                    • Leg Extension
                    • Calf Raise

                    Chọn mức tạ mà bạn có thể kiểm soát kỹ thuật tốt.
                    """;
            }

            else if (ContainsAny(
                question,
                "phục hồi",
                "đau cơ",
                "nghỉ"))
            {
                response =
                    """
                    Phục hồi là một phần của tập luyện.

                    Bạn nên ngủ đủ, có thời gian nghỉ giữa các buổi tập và tránh tập nặng liên tục cùng một nhóm cơ.

                    Nếu có đau bất thường, đau sắc hoặc kéo dài, nên dừng bài tập và trao đổi với chuyên gia y tế.
                    """;
            }

            else
            {
                response =
                    """
                    Phiên bản Demo AI PT hiện hỗ trợ các nội dung như:

                    • Giáo án của tôi
                    • BMI của tôi
                    • Mục tiêu tập luyện
                    • Tăng cơ / giảm mỡ
                    • Tập ngực / lưng / chân
                    • Phục hồi sau tập

                    Bạn hãy thử hỏi một trong các nội dung trên.
                    """;
            }

            return Task.FromResult(response);
        }


        private static string BuildBmiResponse(
            Customer customer)
        {
            if (!customer.Height.HasValue ||
                !customer.Weight.HasValue ||
                customer.Height.Value <= 0 || customer.Weight.Value <= 0)
            {
                return
                    "Bạn chưa cập nhật đầy đủ chiều cao " +
                    "và cân nặng.";
            }

            var height =
                (double)customer.Height.Value / 100;

            var bmi =
                (double)customer.Weight.Value /
                (height * height);

            return
                $"Chiều cao: {customer.Height} cm\n" +
                $"Cân nặng: {customer.Weight} kg\n" +
                $"BMI ước tính: {bmi:0.0}\n\n" +
                "BMI chỉ là chỉ số tham khảo và không phản ánh " +
                "đầy đủ thành phần cơ thể.";
        }


        private static string BuildGoalResponse(
            Customer customer)
        {
            return customer.FitnessGoal switch
            {
                "MUSCLE_GAIN" =>
                    "Mục tiêu của bạn hiện tại là tăng cơ.",

                "WEIGHT_LOSS" =>
                    "Mục tiêu của bạn hiện tại là giảm cân/giảm mỡ.",

                "MAINTAIN" =>
                    "Mục tiêu của bạn hiện tại là duy trì thể lực.",

                _ =>
                    "Bạn chưa cập nhật mục tiêu tập luyện."
            };
        }


        private static string BuildWorkoutResponse(
            WorkoutPlan? plan)
        {
            if (plan == null)
            {
                return
                    "Bạn chưa có giáo án. Hãy vào mục Giáo án " +
                    "và sử dụng chức năng tạo Workout Plan.";
            }

            var builder =
                new StringBuilder();

            builder.AppendLine(
                $"Giáo án hiện tại: {plan.Title}"
            );

            if (!string.IsNullOrWhiteSpace(
                plan.PredictedPlanType))
            {
                builder.AppendLine(
                    $"Loại: {plan.PredictedPlanType}"
                );
            }

            if (plan.WorkoutExercises != null &&
                plan.WorkoutExercises.Any())
            {
                var days =
                    plan.WorkoutExercises
                        .OrderBy(x => x.DayNumber).ThenBy(x => x.OrderNumber)
                        .GroupBy(x => x.DayNumber);

                foreach (var day in days)
                {
                    builder.AppendLine();
                    builder.AppendLine(
                        $"Ngày {day.Key}:"
                    );

                    foreach (var item in day.Take(5))
                    {
                        builder.AppendLine(
                            $"- {item.Exercise.Name}: " +
                            $"{item.Sets} sets × {item.Reps}"
                        );
                    }
                }
            }

            return builder.ToString();
        }


        private static bool ContainsAny(
            string input,
            params string[] keywords)
        {
            return keywords.Any(
                keyword => System.Text.RegularExpressions.Regex.IsMatch(input,
                    @"(?<![\p{L}\p{N}])" + System.Text.RegularExpressions.Regex.Escape(keyword) + @"(?![\p{L}\p{N}])")
            );
        }
    }
}
