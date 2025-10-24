namespace CollisionBear.WorldEditor.Generation
{
    public interface IGenerationBounds
    {
        bool IsWithinBounds(float size, BoxRect box);
    }
}
