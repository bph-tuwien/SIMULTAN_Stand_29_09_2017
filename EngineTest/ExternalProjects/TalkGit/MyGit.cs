using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using LibGit2Sharp.Handlers;
using LibGit2Sharp;
using MyDataTypes;





namespace TalkGit
{
    #region GALINA code

    public class CommitRecord
    {
        public string Key { get; private set; }
        public string MsgShort { get; private set; }
        public string MsgLoong { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public string AuthorName { get; private set; }

        public CommitRecord(string _key, string _msg_short, string _msg_loong, DateTime _time_stamp, string _author_name)
        {
            this.Key = _key;
            this.MsgShort = _msg_short;
            this.MsgLoong = _msg_loong;
            this.TimeStamp = _time_stamp;
            this.AuthorName = _author_name;
        }
    }

    #endregion

    public class MyGit
    {
        #region SAVE
        public string Save(string pathLocalRepo, string fileName, string content, string author, string email, DateTime commitTime, string commitMessage, string userName, string passWord, bool calcChanged)
        {
            using (var repo = new LibGit2Sharp.Repository(pathLocalRepo))
            {
                // Commit
                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, fileName), content);

                Commands.Stage(repo, "*"); // added jede Änderung 

                // Create the committer's signature and commit
                Signature authorHelp = new Signature(author, email, commitTime);
                Signature committer = authorHelp;

                // Commit to the repository
                try
                {
                    if (calcChanged == true)// CalcChanged Flag nach dem später gefiltert wird
                    {
                        Commit commit = repo.Commit("CalcChanged:       " + commitMessage, authorHelp, committer); // Bei keinen Änderungen im Commit hängt es Ihn hier auf, exceptionhandling einbauen
                    }
                    else
                    {
                        Commit commit = repo.Commit(commitMessage, authorHelp, committer); // Bei keinen Änderungen im Commit hängt es Ihn hier auf, exceptionhandling einbauen
                    }

                    // Pushen in RemoteRepository
                    Remote remote = repo.Network.Remotes["origin"]; // hier gehts auf die Server-Adresse

                    var options = new PushOptions();
                    options.CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials { Username = userName, Password = passWord };
                    repo.Network.Push(remote, @"refs/heads/master", options);
                }
                catch (Exception e)
                {
                    return e.ToString();
                }

            }

            return "ok";
        }
        #endregion

        #region FORWARD <-> BACKWARD
        //C:\Users\Christoph Handler\Documents\librepo\, für lokal
        //public string Backwards(string lastCheckout, string pathLocalRepo, string userName, string passWord, string projectKey, string repoName)
        public string Backwards(string lastCheckout, string pathLocalRepo)// alle Infos sind eh im Git-Ordner vorhanden
        {
            List<string> CommitIDList = new List<string>(); // Liste mit allen Commits die wir haben, beim forward einfach Liste umdrehen

            using (var repo = new Repository(pathLocalRepo))
            {
                var helpList = repo.Commits.ToList();// mit dem property DateTime haben wir die Commit-Zeite
                
                foreach (var x in helpList)
                {
                    CommitIDList.Add(x.Sha.Substring(0, 7));
                }
            }

            CommitIDList.Reverse(); // Liste ist umgekehrt

            List<MyCommit> ReverseList = new List<MyCommit>(); // passt haben jetzt überall Parentwerte
            string oldValue = string.Empty;

            foreach(string e in CommitIDList)
            {

                MyCommit rL = new MyCommit
                {

                    CommitID = e,
                    ParentCommitID = oldValue
                };
                oldValue = e;
                ReverseList.Add(rL);
            }

            if (String.IsNullOrEmpty(lastCheckout))
            {
                CommitIDList.Reverse();
                lastCheckout = CommitIDList.First();
                
            }


            ReverseList.Reverse();

            foreach (MyCommit c in ReverseList)
            {
                if (c.CommitID == lastCheckout)
                { 
                     using (var repo = new Repository(pathLocalRepo))
                     {
                         var checkoutPaths = new[] { "*" };
                         CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
                         if (String.IsNullOrEmpty(c.ParentCommitID))
                         {
                             c.ParentCommitID = c.CommitID; // kein weiteres Rollback mehr möglich
                         }
                         repo.CheckoutPaths(c.ParentCommitID, checkoutPaths, options); // commented out for testing                        
                         lastCheckout = c.ParentCommitID;
                     }
                     break; // Commented out for testing
                }
            }


            return lastCheckout;

        }


        //public string Forwards(string lastCheckout, string pathLocalRepo, string userName, string passWord, string projectKey, string repoName)
        public string Forwards(string lastCheckout, string pathLocalRepo)
        {
            List<string> CommitIDList = new List<string>(); // Liste mit allen Commits die wir haben, beim forward einfach Liste umdrehen

            using (var repo = new Repository(pathLocalRepo))
            {
                var helpList = repo.Commits.ToList();

                foreach (var x in helpList)
                {
                    CommitIDList.Add(x.Sha.Substring(0, 7));
                }
            }

            CommitIDList.Reverse(); // Liste nicht umgekehren

            List<MyCommit> ReverseList = new List<MyCommit>(); // passt haben jetzt überall Parentwerte
            string oldValue = string.Empty;

            foreach (string e in CommitIDList)
            {

                MyCommit rL = new MyCommit
                {
                    ParentCommitID = e,
                    CommitID = oldValue

                    
                };
                
                oldValue = e;
                ReverseList.Add(rL);
            }

            if (String.IsNullOrEmpty(lastCheckout))
            {
                CommitIDList.Reverse();
                lastCheckout = CommitIDList.First();

            }


            //ReverseList.Reverse(); // hier glaube ich nicht umdrehen

            // Erzeugen einer Liste abgeschlossen




            foreach (MyCommit c in ReverseList)
            {
                if (c.CommitID == lastCheckout)
                {
                    using (var repo = new Repository(pathLocalRepo))
                    {
                        var checkoutPaths = new[] { "*" };
                        CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
                        if (String.IsNullOrEmpty(c.ParentCommitID))
                        {
                            c.ParentCommitID = c.CommitID; // kein weiteres Rollback mehr möglich
                        }
                        repo.CheckoutPaths(c.ParentCommitID, checkoutPaths, options); // commented out for testing                        
                        
                        lastCheckout = c.ParentCommitID;
                    }
                    break; // Commented out for testing
                }
            }


            return lastCheckout;

        }
        #endregion

        //public string GoToVersion(string lastCheckout, string pathLocalRepo, string userName, string passWord, string projectKey, string repoName)
        public string GoToVersion(string lastCheckout, string pathLocalRepo)
        {
            string result = string.Empty;

            try
            {
                using (var repo = new Repository(pathLocalRepo))
                {
                    var checkoutPaths = new[] { "*" };
                    CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };

                    repo.CheckoutPaths(lastCheckout, checkoutPaths, options); // commented out for testing                        
                }
                result = "ok";
            }
            catch (Exception exception)
            {
                result = Convert.ToString(exception);
            }

            return result;
        }

        #region OTHER
        public List<ComboboxItem> fillAllItemsComboBox(string pathLocalRepo)
        {
            List<ComboboxItem> fillAllComboboxList = new List<ComboboxItem>();

            using (var repo = new Repository(pathLocalRepo))
            {
                var helpList = repo.Commits.ToList();// mit dem property DateTime haben wir die Commit-Zeite

                foreach(var item in helpList)
                {
                    ComboboxItem ci = new ComboboxItem()
                    {
                        View = Convert.ToString(item.Id).Substring(0, 7) + " " + Convert.ToString(item.Committer.When.DateTime),
                        Value = Convert.ToString(item.Id).Substring(0, 7)
                    };

                    fillAllComboboxList.Add(ci);
                    //MessageBox.Show(Convert.ToString(item.Id).Substring(0, 7) + " " + item.MessageShort + " " + Convert.ToString(calcChanged) + " " + Convert.ToString(item.Committer.When.DateTime));
                }
  
            }

            return fillAllComboboxList;
        }


        public List<ComboboxItem> fillCalcChangedItemsComboBox(string pathLocalRepo)
        {
            List<ComboboxItem> fillCalcChangedComboboxList = new List<ComboboxItem>();

            using (var repo = new Repository(pathLocalRepo))
            {
                var helpList = repo.Commits.ToList();// mit dem property DateTime haben wir die Commit-Zeite

                foreach (var item in helpList)
                {
                    bool calcChanged = Convert.ToString(item.MessageShort).Contains(@"CalcChanged");

                    if (calcChanged == true)
                    {
                        ComboboxItem ci = new ComboboxItem()
                        {
                            View = Convert.ToString(item.Id).Substring(0, 7) + " " + Convert.ToString(item.Committer.When.DateTime),
                            Value = Convert.ToString(item.Id).Substring(0, 7)
                        };

                        fillCalcChangedComboboxList.Add(ci);
                    }

                    
                    //MessageBox.Show(Convert.ToString(item.Id).Substring(0, 7) + " " + item.MessageShort + " " + Convert.ToString(calcChanged) + " " + Convert.ToString(item.Committer.When.DateTime));
                }

            }

            return fillCalcChangedComboboxList;
        }
        #endregion

        #region GALINA code: RetrieveVersionList, Save

        public List<CommitRecord> RetrieveVersionList(string _path_to_local_repo, int _max_nr_items)
        {
            List<CommitRecord> commit_records = new List<CommitRecord>();
            using (var repo = new Repository(_path_to_local_repo))
            {
                // get all commits
                List<Commit> commits = repo.Commits.ToList();
                int counter = 0;
                foreach (Commit c in commits)
                {
                    string author = c.Author.Name;
                    DateTime time_stamp = c.Committer.When.DateTime;
                    string key = Convert.ToString(c.Id).Substring(0, 7);

                    string msg = c.Message;
                    string msg_short = c.MessageShort;

                    CommitRecord cr = new CommitRecord(key, msg_short, msg, time_stamp, author);
                    commit_records.Add(cr);
                    counter++;
                    if (counter == _max_nr_items)
                        break;
                }
            }

            return commit_records;
        }

        // difference to other Save method: no content, but a commit_key as an OUT parameter
        public string Save(string pathLocalRepo, string fileName, string author, string email, DateTime commitTime, string commitMessage, string userName, string passWord, bool calcChanged,
                            out string commit_key)
        {
            commit_key = string.Empty;
            using (var repo = new LibGit2Sharp.Repository(pathLocalRepo))
            {
                // Commit
                Commands.Stage(repo, "*"); // added jede Änderung 

                // Create the committer's signature and commit
                Signature authorHelp = new Signature(author, email, commitTime);
                Signature committer = authorHelp;

                // Commit to the repository
                try
                {
                    if (calcChanged == true)// CalcChanged Flag nach dem später gefiltert wird
                    {
                        Commit commit = repo.Commit("CalcChanged:       " + commitMessage, authorHelp, committer); // Bei keinen Änderungen im Commit hängt es Ihn hier auf, exceptionhandling einbauen
                        commit_key = (commit == null) ? string.Empty : Convert.ToString(commit.Id).Substring(0, 7);
                    }
                    else
                    {
                        Commit commit = repo.Commit(commitMessage, authorHelp, committer); // Bei keinen Änderungen im Commit hängt es Ihn hier auf, exceptionhandling einbauen
                        commit_key = (commit == null) ? string.Empty : Convert.ToString(commit.Id).Substring(0, 7);
                    }

                    // Pushen in RemoteRepository
                    Remote remote = repo.Network.Remotes["origin"]; // hier gehts auf die Server-Adresse

                    var options = new PushOptions();
                    options.CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials { Username = userName, Password = passWord };
                    repo.Network.Push(remote, @"refs/heads/master", options);
                }
                catch (Exception e)
                {
                    return e.ToString();
                }

            }

            return "ok";
        }

        #endregion

        #region PULL: updated 07.09.2017

        public void Pull(string repoPath, string account, string password)
        {
            // https://stackoverflow.com/questions/42659482/using-libgit2sharp-to-pull-latest-from-a-branch
            // http://berghamster@128.130.183.105:7990/scm/proj1/repofortesting.git
            // FetchAll(repoPath, account, password);

            string branchName = this.CheckoutBranch(@"master", repoPath);

            MergeResult result = null;
            if (!string.IsNullOrEmpty(branchName))
                result = this.PullBranch(branchName, repoPath, account, password);
            //PullBranch(@"master", repoPath, account, password); // geht nicht
        }

        public string CheckoutBranch(string branchName, string repoPath)
        {
            using (var repo = new Repository(repoPath))
            {
                var trackingBranch = repo.Branches[branchName];
                if (trackingBranch == null) return null;

                if (trackingBranch.IsRemote)
                {
                    branchName = branchName.Replace("origin/", string.Empty);

                    var branch = repo.CreateBranch(branchName, trackingBranch.Tip);
                    repo.Branches.Update(branch, b => b.TrackedBranch = trackingBranch.CanonicalName);
                    Commands.Checkout(repo, branch, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                }
                else
                {
                    Commands.Checkout(repo, trackingBranch, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                }

                return branchName;
            }
        }

        public MergeResult PullBranch(string branchName, string repoPath, string account, string password)
        {
            using (var repo = new Repository(repoPath))
            {
                PullOptions options = new PullOptions();

                options.MergeOptions = new MergeOptions();
                options.MergeOptions.FailOnConflict = true;

                options.FetchOptions = new FetchOptions();
                options.FetchOptions.CredentialsProvider = new CredentialsHandler((url, usernameFromUrl, types) => new UsernamePasswordCredentials()
                {
                    Username = account,
                    Password = password
                });

                // repo.Network.Pull(new Signature(account, password, new DateTimeOffset(DateTime.Now)), options); // obsolete method
                MergeResult result = Commands.Pull(repo, new Signature(account, password, new DateTimeOffset(DateTime.Now)), options);
                return result;
            }
        }


        #endregion

        #region STATUS CHECK


        public string GetSatusOfRepo(string repoPath)
        {
            string status = string.Empty;
            using (var repo = new Repository(repoPath))
            {
                StatusOptions options = new StatusOptions();
                options.IncludeUnaltered = true;
                options.IncludeIgnored = true;
                options.ExcludeSubmodules = false;
                options.RecurseIgnoredDirs = true;
                options.Show = StatusShowOption.IndexAndWorkDir;

                foreach(var item in repo.RetrieveStatus(options))
                {
                    status += item.FilePath + ": " + item.State.ToString() + "\n";
                }

                foreach(var branch in repo.Branches)
                {
                    status += "Branch " + branch.CanonicalName + ": ";
                    if (branch.TrackingDetails != null)
                    {
                        if (branch.TrackingDetails.AheadBy.HasValue)
                            status += "ahead by " + branch.TrackingDetails.AheadBy.Value + ", ";
                        if (branch.TrackingDetails.BehindBy.HasValue)
                            status += "behind by " + branch.TrackingDetails.BehindBy.Value + ".";
                    }
                    status += "\n";
                }
            }

            return status;
        }

        public int GetNrStepsBehindRemoteBranch(string repoPath)
        {
            int nr_steps_behind = 0;
            using (var repo = new Repository(repoPath))
            {
                foreach (var branch in repo.Branches)
                {
                    if (branch.TrackingDetails != null)
                    {
                        if (branch.TrackingDetails.BehindBy.HasValue)
                            nr_steps_behind += branch.TrackingDetails.BehindBy.Value;
                    }
                }
            }
            return nr_steps_behind;
        }


        public string GetStatusOfRemoteRepo(string repoPath, string account, string password)
        {
            string status = string.Empty;
            using (var repo = new Repository(repoPath))
            {
                FetchOptions options = new FetchOptions();
                options.CredentialsProvider = new CredentialsHandler((url, user_name_from_url, type) =>               
                    new UsernamePasswordCredentials()
                    {
                        Username = account,
                        Password = password
                    });

                foreach(Remote remote in repo.Network.Remotes)
                {
                    IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, options, status);
                }
            }
            return status;
        }

        #endregion

        #region TEST

        public void test_1()
        {
            using (var repo = new Repository(@"C:\Users\Christoph Handler\Documents\Visual Studio 2013\Projects\RemoteRepository\"))
            {
                //52b913b
                var commit = repo.Lookup<Commit>("dbf00f8");

                //var x = commit.Committer.When.TimeOfDay; // Uhrzeit des commits

                



                var treeEntry = commit[@"C:\Users\Christoph Handler\Documents\Visual Studio 2013\Projects\RemoteRepository\Calc_MeinAufsatz.txt"];
                var blob = (Blob)treeEntry.Target;
                var contentStream = blob.GetContentStream();

                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();
                    MessageBox.Show("content: " + content);
                }
            }
        }

        #endregion

    }
}
