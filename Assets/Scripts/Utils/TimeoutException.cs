using System;

public class TimeoutException : Exception
{
    public TimeoutException() : base() { }
    public TimeoutException(string message) : base(message) { }
    public TimeoutException(string message, Exception inner) : base(message, inner) { }
}
