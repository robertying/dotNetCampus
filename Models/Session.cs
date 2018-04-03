namespace CampusNet
{
    public class Session : ObservableObject
    {
        private string id;
        private string ip;
        private string deviceName;
        private string usage;
        private string startTime;

        public string ID
        {
            get => id;
            set
            {
                if (value != id)
                {
                    id = value;
                    OnPropertyChanged("ID");
                }
            }
        }

        public string IP
        {
            get => ip;
            set
            {
                if (value != ip)
                {
                    ip = value;
                    OnPropertyChanged("IP");
                }
            }
        }

        public string DeviceName
        {
            get => deviceName;
            set
            {
                if (value != deviceName)
                {
                    deviceName = value;
                    OnPropertyChanged("DeviceName");
                }
            }
        }

        public string Usage
        {
            get => usage;
            set
            {
                if (value != usage)
                {
                    usage = value;
                    OnPropertyChanged("Usage");
                }
            }
        }

        public string StartTime
        {
            get => startTime;
            set
            {
                if (value != startTime)
                {
                    startTime = value;
                    OnPropertyChanged("StartTime");
                }
            }
        }
    }
}
