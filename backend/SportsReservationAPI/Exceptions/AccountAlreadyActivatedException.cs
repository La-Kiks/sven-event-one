namespace SportsReservationAPI.Exceptions
{
    public class AccountAlreadyActivatedException : Exception
    {
        public AccountAlreadyActivatedException(string message) : base(message) { }
    }
}
