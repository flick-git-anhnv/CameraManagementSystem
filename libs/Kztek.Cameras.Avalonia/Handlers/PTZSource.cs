using System;
using System.Net;
using System.Threading;

namespace Kztek.Cameras
{
    public class PTZSource
    {
        private string source;
        private string login;
        private string password;
        private bool useSeparateConnectionGroup;
        private bool preventCaching = true;
        private int requestTimeout = 5000;
        private Thread thread;
        private ManualResetEvent stopEvent;
        public string Source
        {
            get { return this.source; }
            set { this.source = value; }
        }
        public string Login
        {
            get { return this.login; }
            set { this.login = value; }
        }
        public string Password
        {
            get { return this.password; }
            set { this.password = value; }
        }
        public bool SeparateConnectionGroup
        {
            get { return this.useSeparateConnectionGroup; }
            set { this.useSeparateConnectionGroup = value; }
        }
        public bool PreventCaching
        {
            get { return this.preventCaching; }
            set { this.preventCaching = value; }
        }
        public int RequestTimeout
        {
            get { return this.requestTimeout; }
            set { this.requestTimeout = value; }
        }
        public bool Running
        {
            get
            {
                if (this.thread != null)
                {
                    if (!this.thread.Join(0))
                    {
                        return true;
                    }
                    this.Free();
                }
                return false;
            }
        }
        public PTZSource()
        {
        }
        public PTZSource(string source)
        {
            this.source = source;
        }
        public void Start()
        {
            if (this.thread == null && !this.Running)
            {
                this.stopEvent = new ManualResetEvent(false);
                this.thread = new Thread(new ThreadStart(this.WorkerThread));
                this.thread.Name = this.source;
                this.thread.Start();
            }
        }
        public void SignalToStop()
        {
            if (this.thread != null)
            {
                this.stopEvent.Set();
            }
        }
        public void WaitForStop()
        {
            if (this.thread != null)
            {
                this.thread.Join();
                this.Free();
            }
        }
        public void Stop()
        {
            if (this.Running)
            {
                // NOTE: Thread.Abort() throws PlatformNotSupportedException on .NET 5+.
                // Behavior parity với bản WinForms (cũng chạy net8.0 và cũng bị lỗi này).
                // Cân nhắc refactor sang CancellationToken pattern ở G5.99 review.
#pragma warning disable SYSLIB0006
                this.thread.Abort();
#pragma warning restore SYSLIB0006
                this.WaitForStop();
            }
        }
        private void Free()
        {
            this.thread = null;
            this.stopEvent.Close();
            this.stopEvent = null;
        }
        public void WorkerThread()
        {
            HttpWebRequest req = null;
            WebResponse resp = null;
            Random rnd = new Random((int)DateTime.Now.Ticks);
            try
            {
                if (!this.preventCaching)
                {
                    req = (HttpWebRequest)WebRequest.Create(this.source);
                }
                else
                {
                    req = (HttpWebRequest)WebRequest.Create(string.Concat(new object[]
                    {
                        this.source,
                        (this.source.IndexOf('?') == -1) ? '?' : '&',
                        "fake=",
                        rnd.Next().ToString()
                    }));
                }
                req.Timeout = this.requestTimeout;
                if (this.login != null && this.password != null && this.login != "")
                {
                    req.Credentials = new NetworkCredential(this.login, this.password);
                }
                if (this.useSeparateConnectionGroup)
                {
                    req.ConnectionGroupName = this.GetHashCode().ToString();
                }
                resp = req.GetResponse();
            }
            catch (WebException)
            {
                Thread.Sleep(250);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (req != null)
                {
                    req.Abort();
                    req = null;
                }
                if (resp != null)
                {
                    resp.Close();
                    resp = null;
                }
            }
        }
    }
}
