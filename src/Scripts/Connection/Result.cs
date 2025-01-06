namespace TD.Connection;

public class Result
{
	public string error;
}

public class Result<T> : Result
{
	public T data;
}