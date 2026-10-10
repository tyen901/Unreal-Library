namespace UELib.Core
{
    /// <summary>
    ///     Implements UPrimitive/Engine.Primitive
    /// </summary>
    [UnrealRegisterClass]
    public class UPrimitive : UObject
    {
        public UBox BoundingBox;
        public USphere BoundingSphere;

        public UPrimitive()
        {
            ShouldDeserializeOnDemand = true;
        }

        /// <summary>
        ///     False when a subclass serializes its own bounds in place of BoundingBox and BoundingSphere.
        /// </summary>
        protected virtual bool HasPrimitiveBounds => true;

        protected override void Deserialize()
        {
            base.Deserialize();

            if (!HasPrimitiveBounds)
            {
                return;
            }

            _Buffer.ReadStruct(out BoundingBox);
            Record(nameof(BoundingBox), BoundingBox);

            _Buffer.ReadStruct(out BoundingSphere);
            Record(nameof(BoundingSphere), BoundingSphere);
        }
    }
}
