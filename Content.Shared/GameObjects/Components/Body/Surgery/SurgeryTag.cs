namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    public readonly struct SurgeryTag
    {
        public string Id { get; init; }

        public static implicit operator string(SurgeryTag tag)
        {
            return tag.Id;
        }

        public static implicit operator SurgeryTag(string str)
        {
            return new() {Id = str};
        }
    }
}
