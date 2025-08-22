namespace YachtCRM.Application.Interfaces
{
    public interface IPredictionService
    {
        float PredictDelayDays(float length, float basePrice, int numTasks, int changeRequests, int interactions);
    }
}
