namespace forum.Exceptions
{
    public class LoginFailedException : Exception
    {
        public LoginFailedException() : base("Login or password is wrong")
        { }
    }
}
