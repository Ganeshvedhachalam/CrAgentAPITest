using System;
using Capillary.ShellProxy.Model;

namespace Capillary.ShellProxy.Service
{
    public interface ICmsService
    {
        string GetMessage(string id);
    }

    public class CmsService : ICmsService
    {
        public string GetMessage(string id)
        {
            throw new NotImplementedException();
        }
    }
}