namespace AuthService.DTO
{
    public class RegisterRequest
    {
        public string Email { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public static class RegisterRequestExample
    {
        public static RegisterRequest[] GetExamples()
        {
            return new RegisterRequest[]
            {
                new RegisterRequest
                {
                    Email = "test@test.com",
                    UserName = "viggo",
                    Password = "Password123!",
                    FirstName = "John",
                    LastName = "Doe"
                }
            };
        }
    }
}
