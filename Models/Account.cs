using System;
using System.Collections.ObjectModel;

namespace CampusNet
{
    public class Account : ObservableObject
    {
        private string username;
        private string password;
        private string usage;
        private string balance;
        private ObservableCollection<Session> sessions;

        public string Username
        {
            get => username;
            set
            {
                if (value != username)
                {
                    username = value;
                    OnPropertyChanged("Username");
                }
            }
        }

        public string Password
        {
            get => password;
            set
            {
                if (value != password)
                {
                    password = value;
                    OnPropertyChanged("Password");
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

        public string Balance
        {
            get => balance;
            set
            {
                if (value != balance)
                {
                    balance = value;
                    OnPropertyChanged("Balance");
                }
            }
        }

        public ObservableCollection<Session> Sessions
        {
            get => sessions;
            set
            {
                if (value != sessions)
                {
                    sessions = value;
                    OnPropertyChanged("Sessions");
                }
            }
        }
    }
}
