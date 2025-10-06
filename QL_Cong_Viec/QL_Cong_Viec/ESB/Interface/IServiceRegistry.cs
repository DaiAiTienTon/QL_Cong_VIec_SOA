namespace QL_Cong_Viec.ESB.Interface
{
    // Ensure there is only one definition of IServiceRegistry in this namespace.
    public interface IServiceRegistry
    {
        void RegisterService(string serviceName, IService service);
        IService GetService(string serviceName);
        List<string> GetAvailableServices();
    }
}
