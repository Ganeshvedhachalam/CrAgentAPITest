using Newtonsoft.Json;
using GEN_Shell_MobileBackend_API.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using cardRes = GEN_Shell_MobileBackend_API.Models;
using custLookUp = GEN_Shell_MobileBackend_API.Models.CustomerLookUp;
using System.Net.Http;
using System.Linq;
using GEN_Shell_MobileBackend_API.Models;

namespace GEN_Shell_MobileBackend_API.Services
{
    public interface VocService
    {
        Task<SmgSurveyResponse> smgSurveyFeedbackAsync(string requestId, SmgSurveyRequest request);
        Task<VocBehavioralEventResponse> behavioralEventTriggerSmgAsync(string requestId, VocBehavioralEventRequest request);
    }
    public class VocSurveyService : VocService
    {
        string _smgServiceUrl; // need to change the name
        Dictionary<string, string> _vocSurveyHeaders;

        public VocSurveyService(string smgServiceUrl)
        {
            _smgServiceUrl = smgServiceUrl;
            _vocSurveyHeaders = new Dictionary<string, string>();
        }
        public VocSurveyService(string smgServiceUrl,Dictionary<string, string> vocSurveyHeaders)
        {
            _smgServiceUrl = smgServiceUrl;
            _vocSurveyHeaders = vocSurveyHeaders;
        }
        public async Task<SmgSurveyResponse> smgSurveyFeedbackAsync(string requestId, SmgSurveyRequest request){
            try
            {
                string url = _smgServiceUrl;
                
                Console.WriteLine("RequestId:{0}. smgSurveyFeedbackAsync.Request Request body:{1}", requestId,
                                                JsonConvert.SerializeObject(request));

                Console.WriteLine("RequestId:{0}. smgSurveyFeedbackAsync.Request", requestId);
                 var smgFeedbackResponse = await HttpHandler.PostAsync<HttpContent, SmgSurveyResponse>(requestId, url,
                                         _vocSurveyHeaders, Helper.CreateStringContent<SmgSurveyRequest>(request), "SmgFeedback");


                Console.WriteLine("RequestId:{0}. smgSurveyFeedbackAsync.Response Response message:{1}", requestId,
                                                JsonConvert.SerializeObject(smgFeedbackResponse));

                return smgFeedbackResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in VocSurveyService.smgSurveyFeedback().Message:'{1}'", requestId, e.Message);
            }

            return default(SmgSurveyResponse);
        }
        public async Task<VocBehavioralEventResponse> behavioralEventTriggerSmgAsync(string requestId, VocBehavioralEventRequest request){
            try
            {
                string url = _smgServiceUrl;
                Console.WriteLine("RequestId:{0}. behavioralEventTriggerSmgAsync.Request", requestId);
                 var vocBehavioralEventResponse = await HttpHandler.PostAsync<HttpContent, VocBehavioralEventResponse>(requestId, url,
                                         _vocSurveyHeaders, Helper.CreateStringContent<VocBehavioralEventRequest>(request), "behavioralEventTriggerSmg");


                Console.WriteLine("RequestId:{0}. behavioralEventTriggerSmgAsync.Response Response message:{1}", requestId,
                                                JsonConvert.SerializeObject(vocBehavioralEventResponse));

                return vocBehavioralEventResponse;

            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in VocSurveyService.behavioralEventTriggerSmgAsync().Message:'{1}'", requestId, e.Message);
            }

            return default(VocBehavioralEventResponse);
        }
    }
}