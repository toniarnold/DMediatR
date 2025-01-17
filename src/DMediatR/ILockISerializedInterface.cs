namespace DMediatR
{
    public class ILockISerializedInterface : SerializedInterface<ILock>
    {
        protected override void Dehydrate(ILock obj)
        {
            obj.HasLocked = null; // SemaphoreSlim is not serializable
        }

        protected override void Rehydrate(ILock obj)
        {
            obj.HasLocked = [];
        }
    }
}