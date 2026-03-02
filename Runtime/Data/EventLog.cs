namespace Neocortex.Data
{
    public struct EventLog
    {
        public EventPriority priority;
        public string date;
        public string content;

        public EventLog(EventPriority priority, string content, string date)
        {
            this.date = date;
            this.content = content;
            this.priority = priority;
        }
    }
}
