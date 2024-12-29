using System;

namespace TD.Exceptions;

public class SingletonException : Exception
{
	public SingletonException(Type type)
		: base($"{type.Name} is a Singleton, use {type.Name}.Instance instead of new {type.Name}()") {}
}