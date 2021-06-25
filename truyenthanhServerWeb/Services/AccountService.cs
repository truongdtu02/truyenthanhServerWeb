using truyenthanhServerWeb.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using truyenthanhServerWeb.ServerMp3;

namespace truyenthanhServerWeb.Services
{
    public class AccountService
    {
        private readonly IMongoCollection<Account> _account;

        public static event System.EventHandler<AccountChangedEventArgs> AccountChanged;

        public void InvokeAccountChangedEvent()
        {
            var newData = Get();
            AccountChanged?.Invoke(this, new AccountChangedEventArgs(newData));
        }

        public AccountService(ITruyenthanhDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _account = database.GetCollection<Account>(settings.AccountCollectionName);
        }

        public bool CheckDuplicateUsername(string newUsername)
        {
            var tmpFind = _account.Find<Account>(account => account.Username == newUsername).FirstOrDefault();
            return tmpFind != null;
        }

        public List<Account> Get() =>
            _account.Find(account => true).ToList();

        public Account Get(string id)
        {
            try { return _account.Find<Account>(account => account.Id == id).FirstOrDefault(); }
            catch { return null; }
        }

        public void Create(Account account)
        {
            if (!CheckDuplicateUsername(account.Username))
            {
                account.Id = null; //Id of MongoDb auto create and manage
                _account.InsertOne(account);

                //add to list user of UDPserver
                var tmpacc = _account.Find<Account>(ac => ac.Username == account.Username).FirstOrDefault();
                if (tmpacc != null) UDPServer._userList.Add(new User() { account = tmpacc });
            }
            InvokeAccountChangedEvent();
        }

        public void Update(string id, Account accountIn)
        {
            var tmpacc = Get(id);
            if (tmpacc != null)
            {
                //check duplicate if username is changed
                if ((accountIn.Username == tmpacc.Username) || (!CheckDuplicateUsername(accountIn.Username))
                    || (accountIn.Username == "" && accountIn.Password == ""))
                {
                    accountIn.Id = tmpacc.Id; //Id of MongoDb auto create and manage, can't change
                    _account.ReplaceOne(account => account.Id == id, accountIn);

                    //update to list user of UDPserver
                    //find index and edit
                    int index = UDPServer._userList.FindLastIndex(u => u.account.Id == id);
                    if (index >= 0)
                        UDPServer._userList[index].account = accountIn;
                }
            }
            InvokeAccountChangedEvent();
        }

        public void Remove(Account accountIn)
        {
            if (Get(accountIn.Id) != null)
            {
                _account.DeleteOne(account => account.Id == accountIn.Id);

                //update to list user of UDPserver
                //find index and edit
                int index = UDPServer._userList.FindLastIndex(u => u.account.Id == accountIn.Id);
                if (index >= 0)
                {
                    //UDPServer._userList[index].killUser();
                    UDPServer._userList.RemoveAt(index);
                }
            }
            InvokeAccountChangedEvent();
        }

        public void Remove(string id)
        {
            if (Get(id) != null)
            {
                _account.DeleteOne(account => account.Id == id);

                //update to list user of UDPserver
                //find index and edit
                int index = UDPServer._userList.FindLastIndex(u => u.account.Id == id);
                if (index >= 0)
                {
                    //UDPServer._userList[index].killUser();
                    UDPServer._userList.RemoveAt(index);
                }
            }
            InvokeAccountChangedEvent();
        }
    }

    public class AccountChangedEventArgs : System.EventArgs
    {
        public List<Account> NewValue { get; }
        //public List<Account> OldValue { get; }

        public AccountChangedEventArgs(List<Account> newValue)
        {
            this.NewValue = newValue;
        }
    }
}
