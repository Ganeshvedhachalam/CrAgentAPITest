using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Models.RsaSha256AndMgf1;
using GEN_Shell_MobileBackend_API.Services;
using GEN_Shell_MobileBackend_API.Utilities;
using Newtonsoft.Json;

namespace GEN_Shell_MobileBackend_API
{
    public class FunctionHandlerValidatePin
    {
        IAuthService _authService;
        IDBService _dynamoService;        
        string authEngineSvcUrl;
        string rsaUtilityUrl;
        string rsaUtilityApiKey;
        IntegrationService _integrationService;
        public FunctionHandlerValidatePin()
        {
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
             if (!string.IsNullOrEmpty(awsRegion))
            {
                authEngineSvcUrl = Environment.GetEnvironmentVariable("CRM_SVC_URL");
                rsaUtilityUrl = Environment.GetEnvironmentVariable("RSA_UTILITY_URL");
                rsaUtilityApiKey = Environment.GetEnvironmentVariable("RSA_UTILITY_API_KEY");

            }
            else
            {
                authEngineSvcUrl = "https://apac2.api.capillarytech.com";
                rsaUtilityUrl = "https://zl091hf320.execute-api.ap-southeast-1.amazonaws.com/demo/biometric/rsa-util";
                rsaUtilityApiKey = "";
            }
            _authService = new AuthEngineService(authEngineSvcUrl);
            _dynamoService = new DynamoService();
            _integrationService = new IntegrationServices(rsaUtilityUrl);
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        public APIGatewayProxyResponse ValidatePin(APIGatewayProxyRequest request, ILambdaContext context)
        {
            request.Headers.TryGetValue("X-Cap-OrgId", out string OrgID);
            request.Headers.TryGetValue("X-Cap-Environment", out string Environment);
            request.Headers.TryGetValue("X-CAP-USE-NEW-ENCRYPTION", out string RsaEcbOaepEnabled);

            bool isRsaEcbOaepMgf1Enabled = !string.IsNullOrEmpty(RsaEcbOaepEnabled) && RsaEcbOaepEnabled.ToLower() == "true" ? true : false;
            string requestId = Guid.NewGuid().ToString("N");
            string response = string.Empty;
            string privateKey=string.Empty;
            string decryptedPassword=string.Empty;
            bool registeredUser=true;
            PinDecryptionResponse decryptionResponse= new PinDecryptionResponse();
            RSADecryption rSADecryption = new RSADecryption();
            GenerateMFATokenRequest generateMFATokenRequest = new GenerateMFATokenRequest();
            var Auth = Helper.API_Authentication(requestId, request);
            if (Auth != "success")
                return funcSendResponse(requestId, HttpStatusCode.Unauthorized, JsonConvert.SerializeObject(new APIErrorClass { message = Auth }), 401);
            try
            {
                var mobileKeysJson = _dynamoService.GetMobileConfigsAsync(requestId, OrgID, Environment).Result;
                if (string.IsNullOrEmpty(mobileKeysJson))
                {
                    decryptionResponse.responseCode = 301;
                    decryptionResponse.responseMessage = "Org Not Found";
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(decryptionResponse), 400);
                }
                var mobileKeys = JsonConvert.DeserializeObject<DBResponseModel>(mobileKeysJson);
                var integrationKeys = mobileKeys.artifacts.Where(c => c.source == "integrations").FirstOrDefault();
                if (integrationKeys == null)
                {
                    decryptionResponse.responseCode=301;
                    decryptionResponse.responseMessage = "integrations keys not found for this org";
                    return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptionResponse), 200);    
                }

                PinDecryptionRequest pinDecryptionRequest = JsonConvert.DeserializeObject<PinDecryptionRequest>(request.Body);
                Console.WriteLine("Validate Pin Request Received: {0}", JsonConvert.SerializeObject(pinDecryptionRequest));
                
                if(string.IsNullOrEmpty(pinDecryptionRequest.authorizedToken)||string.IsNullOrEmpty(pinDecryptionRequest.brand)||string.IsNullOrEmpty(pinDecryptionRequest.deviceId)||
                    string.IsNullOrEmpty(pinDecryptionRequest.encryptedPassword)||string.IsNullOrEmpty(pinDecryptionRequest.identifierValue))
                {
                    decryptionResponse.responseCode=302;
                    decryptionResponse.responseMessage="Invalid request";
                    return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(decryptionResponse), 401);
                }

                if (isRsaEcbOaepMgf1Enabled) //OAEP with SHA-256 & MGP1 padding
                {
                    Console.WriteLine("RequestId:{0}. 'OAEP with SHA-256 MGP1 Padding' is detected.", requestId);
                    if (string.IsNullOrEmpty(pinDecryptionRequest.encryptedPassword))
                    {
                        decryptionResponse.responseCode = 302;
                        decryptionResponse.responseMessage = "Invalid request";
                        return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(decryptionResponse), 400);
                    }
                    
                    CryptoRequest cryptoRequest = new CryptoRequest(pinDecryptionRequest.encryptedPassword);                    
                    var decryptedApiResponse = _integrationService.CryptographicFunction(requestId, cryptoRequest, false, rsaUtilityApiKey, OrgID, Environment).Result;                    
                    if (decryptedApiResponse != null && decryptedApiResponse.status)
                    {
                        decryptedPassword = decryptedApiResponse.decryptedData;
                        Console.WriteLine("RequestId:{0}.Decryped Password: {1} ", requestId, decryptedPassword);
                    }
                    else
                        Console.WriteLine("RequestId:{0}.Decryption Failed: {1} ", requestId);
                }

                if (!isRsaEcbOaepMgf1Enabled)  // for the PKCS Padding logic (default)
                {
                    Console.WriteLine("RequestId:{0}. 'PKCS1 Padding is detected.", requestId);
                    privateKey = integrationKeys.Keys.Where(c => c.key == "RSA_Private_Key_PIN").FirstOrDefault().value;
                    if (String.IsNullOrEmpty(privateKey))
                    {
                        decryptionResponse.responseCode = 301;
                        decryptionResponse.responseMessage = "Private key is not configured for this org";
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptionResponse), 200);
                    }
                }
                //Decrypt the password
                decryptedPassword = isRsaEcbOaepMgf1Enabled ? decryptedPassword : rSADecryption.Decrypt(requestId, pinDecryptionRequest.encryptedPassword, privateKey);

                if (string.IsNullOrEmpty(decryptedPassword))
                {
                    decryptionResponse.responseCode = 303;
                    decryptionResponse.responseMessage = "Decryption Failed";
                    return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptionResponse), 200);
                }
                if (!string.IsNullOrEmpty(pinDecryptionRequest.encryptedConfirmPassword))
                {
                    registeredUser = false;
                    generateMFATokenRequest.password = decryptedPassword;
                    generateMFATokenRequest.confirmPassword = decryptedPassword;
                }
                generateMFATokenRequest.authorizedToken = pinDecryptionRequest.authorizedToken;
                generateMFATokenRequest.brand = pinDecryptionRequest.brand;
                generateMFATokenRequest.deviceId = pinDecryptionRequest.deviceId;
                generateMFATokenRequest.identifierType = pinDecryptionRequest.identifierType;
                generateMFATokenRequest.identifierValue = pinDecryptionRequest.identifierValue;

                var generateMFATokenResponse = _authService.GenerateMFATokenAsync(requestId,generateMFATokenRequest).Result;
                if(registeredUser && generateMFATokenResponse!=null && generateMFATokenResponse.status!=null && 
                    generateMFATokenResponse.status.success==true && generateMFATokenResponse.user!=null && 
                    !string.IsNullOrEmpty(generateMFATokenResponse.user.sessionId))
                {
                    ValidateMFAPasswordRequest mFAPasswordRequest = new ValidateMFAPasswordRequest
                    {
                        brand = generateMFATokenRequest.brand,
                        deviceId = generateMFATokenRequest.deviceId,
                        identifierType = generateMFATokenRequest.identifierType,
                        identifierValue = generateMFATokenRequest.identifierValue,
                        sessionId = generateMFATokenResponse.user.sessionId,
                        password = decryptedPassword
                    };
                    var validateMFAPasswordResponse= _authService.ValidateMFAPasswordAsync(requestId,mFAPasswordRequest).Result;
                    if(validateMFAPasswordResponse!=null && validateMFAPasswordResponse.status!=null && validateMFAPasswordResponse.status.success==true)
                    {
                        decryptionResponse.responseCode=300;
                        decryptionResponse.responseMessage="Success";
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptionResponse), 200);
                    }
                    else 
                    {
                        decryptionResponse.responseCode=305;
                        decryptionResponse.responseMessage=validateMFAPasswordResponse!=null&&validateMFAPasswordResponse.status!=null&&!string.IsNullOrEmpty(validateMFAPasswordResponse.status.message)?validateMFAPasswordResponse.status.message:"Validation Failed";
                        return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptionResponse), 302);
                    }
                    
                }
                else if(!registeredUser && generateMFATokenResponse!=null && generateMFATokenResponse.status.success==true)
                {
                    decryptionResponse.responseCode=300;
                    decryptionResponse.responseMessage="Success";
                    return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptionResponse), 200);
                }
                else
                {
                    decryptionResponse.responseCode=(int)ErrorMapper.GetErrorCode(generateMFATokenResponse?.status?.message);
                    decryptionResponse.responseMessage= generateMFATokenResponse!=null&&!string.IsNullOrEmpty(generateMFATokenResponse.status.message)?generateMFATokenResponse.status.message:"MFA Token Generation Failed";
                    return funcSendResponse(requestId, HttpStatusCode.OK, JsonConvert.SerializeObject(decryptionResponse), 304);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("RequestId:{0}. Exception occured while validating pin:{1}",requestId,ex.Message);
                decryptionResponse.responseCode=306;
                decryptionResponse.responseMessage="Internal Error";
                return funcSendResponse(requestId, HttpStatusCode.BadRequest, JsonConvert.SerializeObject(decryptionResponse), 304);
            }
        }

        Func<string, HttpStatusCode, string, int, APIGatewayProxyResponse> funcSendResponse = (requestId, httpStatusCode, body, returnCode) =>
        {
            Console.WriteLine("RequestId:{0}. Response:{1}", requestId, body.Replace(Environment.NewLine, " "));
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)httpStatusCode,
                Headers = new Dictionary<string, string> { { "content-type", "application/json" }, { "INTG-RequestID", requestId } },
                Body = body
            };
        };
    }
}