using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Timers;

namespace InterProcCommunication
{
    public class NotifyingBool : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region VALUE
        private bool value_internal;
        public bool Value
        {
            get { return this.value_internal; }
            set
            {
                this.value_internal = value;
                this.MonitorValue();
                this.RegisterPropertyChanged("Value");
            }
        }

        public bool Value_NotifyOnTrue
        {
            get { return this.value_internal; }
            set
            {
                this.value_internal = value;
                this.MonitorValue();
                if (this.value_internal)
                    this.RegisterPropertyChanged("Value_NotifyOnTrue");
            }
        }

        public bool Value_NotifyOnFalse
        {
            get { return this.value_internal; }
            set
            {
                this.value_internal = value;
                this.MonitorValue();
                if (!this.value_internal)
                    this.RegisterPropertyChanged("Value_NotifyOnFalse");
            }
        }

        private void MonitorValue()
        {
            if (this.value_internal && ReturnToFalseAfterTimeOut)
                this.timer_true.Start();
            else if (!this.value_internal && ReturnToTrueAfterTimeOut)
                this.timer_false.Start();
        }

        #endregion

        #region TIMEOUT

        public static readonly double TIMEOUT = 10000; // in ms

        private Timer timer_true;
        private Timer timer_false;

        public bool ReturnToTrueAfterTimeOut { get; set; }
        public bool ReturnToFalseAfterTimeOut { get; set; }

        #endregion

        public NotifyingBool(bool _init_value)
        {
            this.value_internal = _init_value;

            this.timer_true = new Timer(TIMEOUT);
            this.timer_true.Elapsed += timer_true_Elapsed;
            this.timer_true.AutoReset = false;

            this.timer_false = new Timer(TIMEOUT);
            this.timer_false.Elapsed += timer_false_Elapsed;
            this.timer_false.AutoReset = false;

            this.ReturnToTrueAfterTimeOut = false;
            this.ReturnToFalseAfterTimeOut = false;
        }


        #region EVENT HANDLERS

        private void timer_true_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.timer_true.Stop();
            this.value_internal = false;
            this.RegisterPropertyChanged("FalseAfterTimeout");
        }

        private void timer_false_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.timer_false.Stop();
            this.value_internal = true;
            this.RegisterPropertyChanged("TrueAfterTimeout");
        }

        #endregion
    }
}
