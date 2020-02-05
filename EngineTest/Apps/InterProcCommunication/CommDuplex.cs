using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Runtime.CompilerServices;

namespace InterProcCommunication
{
    public class CommDuplex
    {
        #region STATIC

        protected static int numThreads = 1;
        protected static int numCharInLogMsg = 100;
        protected const string CLIENT_CLOSING_MSG = "CLIENT_CLOSING";
        protected const string SERVER_CLOSING_MSG = "SERVER_CLOSING";
        protected const string SERVER_NOT_FOUND = "NOT_FOUND";

        #endregion

        #region CLASS MEMBERS

        public string Name { get; protected set; }

        protected string pipe_name_client;
        protected string pipe_name_server;

        protected string authentication;

        protected Task t_server;
        protected Task t_client;
        protected ManualResetEvent mre_server;

        protected NamedPipeServerStream pipe_server; // for answering other CommDuplex instances
        protected NamedPipeClientStream pipe_client; // for sending requests to other CommDuplex instances

        protected StreamString ss_server;
        protected StreamString ss_client;

        public event ThreadChangedEventHandler ServerChanged;
        public event ThreadChangedEventHandler ClientChanged;

        // for feeding the client thread
        private string current_user_input;
        public string CurrentUserInput 
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get { return this.current_user_input; }
            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                this.current_user_input = value;
                this.ewh.Set(); // added 21.05.2017
            }
        }

        public LocalLogger LoggingService { get; protected set; }

        protected bool closing_initiated_by_client;
        protected bool stop;
        protected bool stop_server;
        public bool IsStopped { get { return this.stop; } }

        // delegate for handling different types of requests
        public delegate string AnswerRequest(string _request);
        // the handler can be set by the caller: so that the server can do 
        // a different task depending on the caller
        public AnswerRequest AnswerRequestHandler { get; set; }

        // DEBUG
        private int debug_client_counter;
        private int debug_server_counter;
        // optimization of the while loop - added 21.05.2017
        private EventWaitHandle ewh;

        #endregion

        #region .CTOR

        public CommDuplex(string _name, string _pipe_name_client, string _pipe_name_server, string _authentication,
                          LocalLogger _logger)
        {
            this.LoggingService = _logger;

            this.Name = _name;
            this.pipe_name_client = _pipe_name_client;
            this.pipe_name_server = _pipe_name_server;
            this.authentication = _authentication;

            this.stop = true;
            this.stop_server = true;

            this.LoggingService.LogCommUnit("Started CommUnit {0}", this.Name);
        }

        public override string ToString()
        {
            return this.Name + ": stopped " + this.stop;
        }

        #endregion


        #region METHODS: COMMUNICATION

        public void StartDuplex()
        {
            this.stop = false;
            this.LoggingService.LogCommUnit( "*** CommDuplex {0} ***", this.Name);

            // start the communication server thread
            //this.LoggingService.LogCommUnit( "Starting server \"{0}\" task ...", this.pipe_name_server);
            this.mre_server = new ManualResetEvent(false);
            this.t_server = new Task(() => this.ServerThread(null));
            this.t_server.Start();
            this.t_server.ContinueWith(this.AfterServerTaskIsDone, CancellationToken.None);
            this.stop_server = false;

            // start the communication client thread
            //this.LoggingService.LogCommUnit("Starting client of server \"{0}\" task ...", this.pipe_name_client);
            this.ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
            this.t_client = new Task(() => this.ClientThread(null));
            this.t_client.Start();
            this.t_client.ContinueWith(this.AfterClientTaskIsDone, CancellationToken.None);
        }

        public void StopDuplex(bool _closing_initiated_by_other)
        {
            if (this.stop) return;
            this.stop = true;

            // signal the server thread if blocked
            this.mre_server.Set();

            if (this.pipe_client != null)
            {
                if (_closing_initiated_by_other)
                {
                    this.LoggingService.LogCommUnit("Closing CommDuplex Client of {0} on request...", this.pipe_name_client);
                }
                else
                {
                    this.LoggingService.LogCommUnit("Closing CommDuplex Client of {0}...", this.pipe_name_client);
                    if (this.ss_client != null)
                        this.ss_client.WriteString(CommDuplex.CLIENT_CLOSING_MSG);
                }

                this.pipe_client.Close();
                this.pipe_client = null;
                if (this.ClientChanged != null)
                    this.ClientChanged(this, TChangedEventArgs.TERMINATED);
            }

            if (this.pipe_server != null)
            {
                if (_closing_initiated_by_other)
                    this.LoggingService.LogCommUnit("Closing CommDuplex Server {0} on request...", this.pipe_name_server);
                else
                    this.LoggingService.LogCommUnit("Closing CommDuplex Server {0}...", this.pipe_name_server);

                this.pipe_server.Close();
                this.pipe_server = null;
                if (this.ServerChanged != null)
                    this.ServerChanged(this, TChangedEventArgs.TERMINATED);
            }

            this.mre_server.Reset();
        }

        private void AfterServerTaskIsDone(Task _t)
        {
            this.stop_server = true;
        }

        private void AfterClientTaskIsDone(Task _t)
        {
            this.StopDuplex(this.closing_initiated_by_client);
        }

        #endregion

        #region SERVER

        protected void ServerThread(object data)
        {
            this.LoggingService.LogServer("Starting server \"{0}\" task ...", this.pipe_name_server);
            this.pipe_server =
                new NamedPipeServerStream(this.pipe_name_server, PipeDirection.InOut, numThreads, 
                                                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            int threadId = Thread.CurrentThread.ManagedThreadId;

            //// Wait for a client to connect (BLOCKING!)
            //this.pipe_server.WaitForConnection();
            // Wait for client to connect (non-Blocking)
            this.pipe_server.WaitForConnectionEx(this.mre_server);
            if (this.stop)
                return;

            this.LoggingService.LogServer("Client connected on thread[{0}].", threadId);

            // Respond to client
            this.closing_initiated_by_client = false;
            try
            {
                // Read the request from the client. Once the client has
                // written to the pipe its security token will be available.

                // prepare for communication
                this.ss_server = new StreamString(this.pipe_server);

                // Verify our identity to the connected client using a
                // string that the client anticipates.
                this.LoggingService.LogServer("Sending authentication: {0}", this.authentication);
                this.ss_server.WriteString(this.authentication);

                while (this.pipe_server != null && !this.stop)
                {
                    // Read the client request.
                    string request = this.ss_server.ReadString();
                    string request_short = CommDuplex.ExtractShortMessage(request);
                    this.LoggingService.LogServer("Read request from client: {0}", request_short);
                    ////this.debug_server_counter++;
                    ////System.Diagnostics.Debug.WriteLine("----------------SERVER " + this.Name + "-----------------[" + Thread.CurrentThread.ManagedThreadId + ":" + this.debug_server_counter + "] Read request from client: " + request_short);
                    if (this.ServerChanged != null)
                        this.ServerChanged(this, TChangedEventArgs.TChangedEventArgsRequest(request));
                    if (request == CommDuplex.CLIENT_CLOSING_MSG)
                    {
                        this.LoggingService.LogServer("Received closing message from client.");
                        this.closing_initiated_by_client = true;
                        break;
                    }
                    // Find answer to the question
                    string answer = CommDuplex.SERVER_NOT_FOUND;
                    if (this.AnswerRequestHandler != null)
                        answer = this.AnswerRequestHandler.Invoke(request);

                    this.ss_server.WriteString(answer);
                    ////System.Diagnostics.Debug.WriteLine("----------------SERVER " + this.Name + "-----------------[" + Thread.CurrentThread.ManagedThreadId + ":" + this.debug_server_counter + "] answers: " + CommDuplex.ExtractShortMessage(answer));
                }
            }
            catch (IOException e)
            {
                this.LoggingService.LogServer("***ERROR: {0}", e.Message);
            }
        }

        #endregion

        #region CLIENT

        protected void ClientThread(object data)
        {
            this.LoggingService.LogClient("Starting client of server \"{0}\" task ...", this.pipe_name_client);
            this.pipe_client =
                    new NamedPipeClientStream(".", this.pipe_name_client,
                        PipeDirection.InOut, PipeOptions.None,
                        TokenImpersonationLevel.Impersonation);

            this.LoggingService.LogClient("Connecting to server...");
            this.pipe_client.Connect(); // non-blocking

            try
            {
                this.ss_client = new StreamString(this.pipe_client);
                // Validate the server's signature string
                if (this.ss_client.ReadString() == this.authentication)
                {
                    // The client security token is sent with the first write.

                    while (this.pipe_client != null && !this.stop && !this.stop_server)
                    {
                        this.ewh.WaitOne(); // added 21.05.2017
                        if (this.CurrentUserInput != null)
                        {
                            // send the request
                            string request_short = CommDuplex.ExtractShortMessage(this.CurrentUserInput);
                            this.LoggingService.LogClient("Sending request: {0}", request_short);
                            ////this.debug_client_counter++;
                            ////System.Diagnostics.Debug.WriteLine("----------------CLIENT " + this.Name + "-----------------[" + Thread.CurrentThread.ManagedThreadId + ":" + this.debug_client_counter + "] Sending request: " + request_short);
                            this.ss_client.WriteString(this.CurrentUserInput);
                            this.CurrentUserInput = null;

                            // Print the answer to the screen.
                            string answer = this.ss_client.ReadString();
                            string answer_short = CommDuplex.ExtractShortMessage(answer);
                            this.LoggingService.LogClient("Received answer: {0}", answer_short);
                            ////System.Diagnostics.Debug.WriteLine("----------------CLIENT " + this.Name + "-----------------[" + Thread.CurrentThread.ManagedThreadId + ":" + this.debug_client_counter + "] Received answer: " + answer_short);
                            if (this.ClientChanged != null)
                                this.ClientChanged(this, TChangedEventArgs.TChangedEventArgsAnswer(answer));
                        }
                        this.ewh.Reset(); // added 21.05.2017
                    }
                }
                else
                {
                    this.LoggingService.LogClient("Server could not be verified.");
                }
            }
            catch(Exception e)
            {
                this.LoggingService.LogClient("***ERROR: {0}", e.Message);
            }
        }

        #endregion

        #region UTILS

        private static string ExtractShortMessage(string _input)
        {
            if (_input == null) return null;

            string request_short = _input.Substring(0, Math.Min(_input.Length, CommDuplex.numCharInLogMsg));
            request_short = request_short.Replace('\n', ' ');
            request_short = request_short.Replace('\v', ' ');
            request_short = request_short.Replace('\r', ' ');

            return request_short;
        }

        #endregion

    }

    
}
