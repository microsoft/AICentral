namespace AICentral.Core;

/// <summary>
/// Type to allow a method to return either a left or a right value
/// </summary>
/// <typeparam name="T">Type of the left value</typeparam>
/// <typeparam name="T1">Type of the right value</typeparam>
public class Either<T, T1>
{
    private readonly T1? _right;
    private readonly T? _left;

    public Either(T value)
    {
        _left = value;
    }

    public Either(T1 value)
    {
        _right = value;
    }

    public bool Left(out T? val)
    {
        val = _left;
        return _left != null;
    }

    public bool Right(out T1? val)
    {
        val = _right;
        return _right != null;
    }
}