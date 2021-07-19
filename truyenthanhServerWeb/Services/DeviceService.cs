using truyenthanhServerWeb.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using truyenthanhServerWeb.ServerMp3;

namespace truyenthanhServerWeb.Services
{
    public class DeviceService
    {
        private readonly IMongoCollection<Device> _device;

        public static event System.EventHandler<DeviceChangedEventArgs> DeviceChanged;

        public void InvokeDeviceChangedEvent()
        {
            DeviceChanged?.Invoke(this, new DeviceChangedEventArgs());
        }

        public DeviceService(ITruyenthanhDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _device = database.GetCollection<Device>(settings.DeviceCollectionName);
        }

        public bool CheckDuplicateId(string id)
        {
            var tmpFind = _device.Find<Device>(dv => dv.Id == id).FirstOrDefault();
            return tmpFind != null;
        }

        //get by index of user in _userList
        public List<Device> GetByIndx(int indx)
        {
            if (indx < 0 || indx >= UDPServer._userList.Count) return null;
            return UDPServer._userList[indx].lDevice;
        }

        public List<Device> Get() =>
            _device.Find(dv => true).ToList();

        public Device Get(string id)
        {
            try { return _device.Find<Device>(dv => dv.Id == id).FirstOrDefault(); }
            catch { return null; }
        }

        //allow input ID for new Device
        public void Create(Device device)
        {
            //check device.OwnerId
            if ((device.OwnerIndx < 0) || (device.OwnerIndx >= UDPServer._userList.Count())) return;
            //check if input Id is not sastified, so Id is mongdb Default
            if ((device.Id == null || device.Id.Length < 24) || (CheckDuplicateId(device.Id))) device.Id = "";

            _device.InsertOne(device);

            //add to list device of user
            UDPServer._userList[device.OwnerIndx].lDevice.Add(device);

            //add to Hasher
            if(!UDPServer._deviceHashet.Add(new Devicemini(device.Id, device.OwnerIndx)))
            {
                //need to synchronize
            }

            InvokeDeviceChangedEvent();
        }

        //on off device
        public bool OnOffDevice(int indx, string dvId, bool stateOnOff)
        {
            if (indx >= 0 && indx < UDPServer._userList.Count)
            {
                int indxDvTmp = UDPServer._userList[indx].lDevice.FindLastIndex(dv => dv.Id == dvId);
                if (indxDvTmp != -1)
                {
                    UDPServer._userList[indx].lDevice[indxDvTmp].deviceEndpoint.On = stateOnOff;
                    return true;
                }
            }
            return false;
        }

        //don't allow update Id and OwnerIndx of Device
        public void Update(string id, Device deviceIn)
        {
            //check device.OwnerId
            if ((deviceIn.OwnerIndx < 0) || (deviceIn.OwnerIndx >= UDPServer._userList.Count())) return;

            var tmpdv = Get(id);
            if (tmpdv != null)
            {
                //check duplicate if id is not changed
                if ((deviceIn.Id == tmpdv.Id) && (deviceIn.OwnerIndx == tmpdv.OwnerIndx))
                {
                    //deviceIn.Id = tmpdv.Id; //Id of MongoDb auto create and manage, can't change
                    _device.ReplaceOne(dv => dv.Id == id, deviceIn);

                    //update to list device of user
                    //find index and edit
                    int index = UDPServer._userList[deviceIn.OwnerIndx].lDevice.FindLastIndex(dv => dv.Id == deviceIn.Id);
                    if (index >= 0)
                        UDPServer._userList[deviceIn.OwnerIndx].lDevice[index] = deviceIn;
                    else
                        UDPServer._userList[deviceIn.OwnerIndx].lDevice.Add(deviceIn); //logic can't happen
                        //compare and synchronize if nessarry
                }
            }
            InvokeDeviceChangedEvent();
        }
        public void Remove(Device deviceIn)
        {
            if (Get(deviceIn.Id) != null)
            {
                _device.DeleteOne(dv => dv.Id == deviceIn.Id);

                //update to list device of user and hashet
                if ((deviceIn.OwnerIndx >= 0) && (deviceIn.OwnerIndx < UDPServer._userList.Count()))
                {
                    UDPServer._userList[deviceIn.OwnerIndx].lDevice.Remove(deviceIn);
                }
                UDPServer._deviceHashet.RemoveWhere(dv => dv.Id == deviceIn.Id);
            }
            InvokeDeviceChangedEvent();
        }

        //reserve, check between mongoDb and (_deviceHashet , _userList[].lDevice)
        public bool CompareWithDb()
        {
            //return true : same, false : have st different 
            return true;
        }
        public void Synchronize()
        {

        }
    }

    public class DeviceChangedEventArgs : System.EventArgs
    {
        //public List<Device> NewValue { get; }

        //public DeviceChangedEventArgs(List<Device> newValue)
        public DeviceChangedEventArgs()
        {
            //this.NewValue = newValue;
        }
    }
}
