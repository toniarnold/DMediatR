namespace DMediatR
{
    public class ILockISerializedInterface : SerializedInterface<ILock>
    {
        protected override void PreSerialize(ILock obj)
        {
            obj.HasLocked = null; // dehydrate as SemaphoreSlim is not serializable
        }

        protected override void PostDeserialize(ILock obj)
        {
            obj.HasLocked = []; // rehydrate
        }
    }
}