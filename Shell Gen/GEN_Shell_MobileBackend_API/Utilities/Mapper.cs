using GEN_Shell_MobileBackend_API.Models;
using GEN_Shell_MobileBackend_API.Models.EmailCommResponse;
using GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp;
using GEN_Shell_MobileBackend_API.Models.Promotions;
using GEN_Shell_MobileBackend_API.Models.TargetDetails;
using GEN_Shell_MobileBackend_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Target = GEN_Shell_MobileBackend_API.Models.TargetDetails.Target;
using Capillary.ShellProxy.Model.CustomerAddModel.Request;

namespace GEN_Shell_MobileBackend_API.Utilities
{
    public class Mapper
    {
        ICrmService _crmService;

        public string Map(string requestId, GetCustomerPromotionResponse getCustomerPromotionResp, string customerId, string intouchSvcUrl, string username, string password)
        {
            try
            {
                _crmService = new IntouchService(intouchSvcUrl, username, password, "");

                MileStoneOffersEndRespose mileStoneOffersEndRespose = new MileStoneOffersEndRespose();
                IList<GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Datum> dataBlockResponse = new List<GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Datum>();
                IList<GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Milestone> milestonesBlock = new List<GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Milestone>();
                IList<GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Milestone> distinctMilestonesBlockResp = new List<GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Milestone>();
                List<string> distinctPromotionIdList = new List<string>();

                int currentDate = Convert.ToInt32(DateTime.UtcNow.ToString("yyyyMMdd"));
                long currentUnixTime = Helper.CurrentUnixTime(requestId);


                // Step 5: Loop through the responseBody of customer promotions API
                foreach (var responseItem in getCustomerPromotionResp.data.Select(selectedData => selectedData))
                {
                    if (responseItem.earnedType.Equals(Constants.MileStoneEarnedType, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("RequestId:{0} MILESTONE_EARN Identified,PromotionId:{1}", requestId, responseItem.promotionId);
                        // Step 5.1: If earnedType = MILESTONE_EARN
                        bool checkExistingMilestone = false;
                        foreach (var milestoneItem in milestonesBlock)
                        {
                            if (milestoneItem.promotionId == responseItem.promotionId)
                            {
                                checkExistingMilestone = true;
                                if (currentUnixTime <= responseItem.validTill)
                                    milestoneItem.availableInstances.Add(SetAvailableInstances(responseItem));
                                else
                                    milestoneItem.expiredInstances.Add(SetExpiredInstances(responseItem));

                                break;
                            }
                        }

                        if (!checkExistingMilestone)
                        {
                            // Step 5.1.2: Add new entry in milestones array
                            var newMilestone = new Milestone
                            {
                                promotionId = responseItem.promotionId,
                                targetGroupId = responseItem.targetGroupId,
                                mileStoneId = responseItem.mileStoneId,
                                milestoneExpiry = responseItem.validTill,
                                promotionName = responseItem.promotionName,
                                imageUrl = responseItem.customFieldValues.ContainsKey("standard_image_1") ? responseItem.customFieldValues["standard_image_1"]:null,
                                availableInstances = new List<Availableinstance>(),
                                expiredInstances = new List<Expiredinstance>(),
                                activeTargetDetails = new Activetargetdetails()
                            };

                            if (currentUnixTime <= responseItem.validTill)
                                newMilestone.availableInstances.Add(SetAvailableInstances(responseItem));
                            else
                                newMilestone.expiredInstances.Add(SetExpiredInstances(responseItem));

                            milestonesBlock.Add(newMilestone);
                        }
                    }
                    else
                    {
                        // Step 5.2: Not a milestone promo, active ones only need to be captured and displayed even if INACTIVE promotion case aswell.
                        if (currentUnixTime <= responseItem.validTill)
                        {
                            Console.WriteLine("RequestId:{0} Valid PromotionId Identified :{1}", requestId, responseItem.promotionId);
                            dataBlockResponse.Add(SetDatum(responseItem));
                        }
                    }
                }

                // Step 6: Select distinct promotionIds from milestones block                
                // Step 6.1: Invoke get Promotion configs API passing all promotionIds in distinctMilestones

                distinctMilestonesBlockResp = milestonesBlock.Where(m => !string.IsNullOrEmpty(m.promotionId)).Select(selected => selected).Distinct().ToList();
                distinctPromotionIdList = milestonesBlock.Select(m => m.promotionId).ToList();

                Console.WriteLine("RequestId:{0}. milestonesBlock ItemCount : {1} distinctPromotionIdList Count:{2} ", requestId, milestonesBlock.Count, distinctPromotionIdList.Count);

                var promotionGetApiDetailsTask = _crmService.PromotionDetailsGetAsync(requestId, distinctPromotionIdList);
                var getTargetDetailsRespTask = _crmService.GetTargetDetailsAsync(requestId, customerId);
                
                var promotionGetApiDetails = promotionGetApiDetailsTask.Result;
                var getTargetDetailsResp = getTargetDetailsRespTask.Result;


                foreach (var distinctMileStoneItem in distinctMilestonesBlockResp)
                {
                    distinctMileStoneItem.activeTargetDetails = null;
                    //Performed only promotionGetApiDetails.data.Count>0
                    if (promotionGetApiDetails != null && promotionGetApiDetails.data != null )
                    {
                        var promotionConfigCurrent = promotionGetApiDetails != null && promotionGetApiDetails.data != null ? promotionGetApiDetails.data.Where(c => c.promotionId == distinctMileStoneItem.promotionId).FirstOrDefault() : null;
                        if (promotionConfigCurrent != null && (currentUnixTime > promotionConfigCurrent.expiry))
                        {
                            Console.WriteLine("RequestId:{0} MileStoneItem promotionId is Expired: {1}", requestId, distinctMileStoneItem.promotionId);
                            // Step 6.1.2: Set distinctMilestone[item].isExpired as true and set activeTargetDetails as null or empty for the promotion
                            distinctMileStoneItem.isExpired = true;
                            distinctMileStoneItem.activeTargetDetails = null;
                            distinctMileStoneItem.milestoneExpiry = promotionConfigCurrent.expiry;
                        }
                        else
                        {
                            distinctMileStoneItem.isExpired = false; // Step 6.1.3: Set isExpired as false
                            distinctMileStoneItem.milestoneExpiry = promotionConfigCurrent != null ? promotionConfigCurrent.expiry : distinctMileStoneItem.milestoneExpiry;
                            distinctMileStoneItem.promotionName = promotionConfigCurrent != null ? promotionConfigCurrent.promotionName: distinctMileStoneItem.promotionName;
                            distinctMileStoneItem.imageUrl = promotionConfigCurrent != null ? promotionConfigCurrent.customFieldValues.standard_image_1: distinctMileStoneItem.imageUrl;
                        }
                            
                    }
                    //Perform this logic whenever getTargetapi response has some values
                    if (distinctMileStoneItem.isExpired == false && getTargetDetailsResp != null && getTargetDetailsResp.data != null)
                    {
                        Console.WriteLine("RequestId:{0} Identified Active PromotionId as IsExpired value is FALSE for targetGroup Api Validation, promotionId:{1}", requestId, distinctMileStoneItem.promotionId);
                        
                        //Search by targetGroupId in responseBody of targetDetails API and save matched targetGroup object against distinctMilestone[item]                        
                        var targetGroup = getTargetDetailsResp.data.targetGroups.Where(t => t.id == distinctMileStoneItem.targetGroupId).FirstOrDefault();
                        if (targetGroup != null)
                        {
                            Console.WriteLine("RequestId:{0} MileStoneItem TargetGroupId :{1}", requestId, distinctMileStoneItem.targetGroupId);
                            var activeTargetDetails = new Activetargetdetails();                            
                            var targetPeriodsForMilestone = new List<Target>();

                            targetGroup.targets.Select(target =>
                            {
                                if (distinctMileStoneItem.mileStoneId == target.targetRuleId)
                                    targetPeriodsForMilestone.Add(target);

                                // Perform operations on item
                                return targetPeriodsForMilestone;
                            }).ToList();

                            #region CYCLIC_WINDOW
                            if (targetGroup.targetEvaluationType.Equals(Constants.MileStoneTargetTypeCyclicWindow, StringComparison.OrdinalIgnoreCase))
                            {                                
                                Console.WriteLine("RequestId:{0} MileStoneItem TargetGroupId :{1} targetEvaluationType: CYCLIC_WINDOW", requestId, distinctMileStoneItem.targetGroupId);
                                
                                targetPeriodsForMilestone.Sort((t1, t2) => t1.periodId.CompareTo(t2.periodId));
                                Console.WriteLine("RequestId:{0} MileStoneItem targetPeriodsForMilestone Sorted with periodId asc,targetPeriodsForMilestone Count:{1}", requestId, targetPeriodsForMilestone.Count);
                                if (targetPeriodsForMilestone.Count > 0)
                                {   
                                    //p1,p2,p3 are pritories in data execution
                                    
                                    if (targetPeriodsForMilestone[0].periodStartDate == targetPeriodsForMilestone[0].periodEndDate && targetPeriodsForMilestone[0].periodStartDate == targetGroup.attribution.createdOn.ToString("yyyy-MM-dd"))
                                    {
                                        Console.WriteLine("RequestId:{0} MileStoneItem periodStartDate == periodEndDate :", requestId);
                                        
                                        activeTargetDetails = SetActiveTargetDetails(targetPeriodsForMilestone[0], targetGroup.targetEvaluationType);                                        
                                        activeTargetDetails.periodStatus = "LOCKED";
                                        activeTargetDetails.targetProgress = targetPeriodsForMilestone[0].targetValue - targetPeriodsForMilestone[0].targetAchievedValue;
                                    } //p2
                                    else if (currentDate > Convert.ToInt32(DateTime.ParseExact(targetPeriodsForMilestone[targetPeriodsForMilestone.Count - 1].periodEndDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")))
                                    {
                                        Console.WriteLine("RequestId:{0} MileStoneItem currentDate == lastPeriodEndDate :", requestId);
                                        activeTargetDetails = null;
                                        distinctMileStoneItem.isExpired = true;
                                    }                  
                                    else //p3
                                    {
                                        bool checkActiveTarget = false;
                                        foreach (var target in targetPeriodsForMilestone)
                                        {
                                            if (Convert.ToInt32(DateTime.ParseExact(target.periodStartDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")) <= currentDate && currentDate <= Convert.ToInt32(DateTime.ParseExact(target.periodEndDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")))
                                            {
                                                activeTargetDetails = SetActiveTargetDetails(targetPeriodsForMilestone[0], targetGroup.targetEvaluationType);
                                                checkActiveTarget = true;

                                                Console.WriteLine("RequestId:{0} MileStoneItem targetValue:{1},targetAchievedValue:{2} :", requestId, target.targetValue, target.targetAchievedValue);
                                                if (target.targetValue > target.targetAchievedValue)
                                                {
                                                    activeTargetDetails.targetProgress = target.targetValue - target.targetAchievedValue;
                                                    activeTargetDetails.periodStatus = "LOCKED";
                                                }
                                                else
                                                {
                                                    activeTargetDetails.targetProgress = 0;
                                                    activeTargetDetails.periodStatus = "UNLOCKED";
                                                }
                                                break;
                                            }
                                        }

                                        Console.WriteLine("RequestId:{0} MileStoneItem checkActiveTarget : {1} ", requestId, checkActiveTarget);

                                        if (!checkActiveTarget)  //no active targets found so checking for upcome events
                                        {
                                            Console.WriteLine("RequestId:{0} MileStoneItem no active targets found so checking for upcome events", requestId);

                                            for (int activePeriodIndex = 0; activePeriodIndex < targetPeriodsForMilestone.Count; activePeriodIndex++)
                                            {
                                                if (Convert.ToInt32(DateTime.ParseExact(targetPeriodsForMilestone[activePeriodIndex].periodEndDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")) > currentDate)
                                                {
                                                    activeTargetDetails = SetActiveTargetDetails(targetPeriodsForMilestone[activePeriodIndex], targetGroup.targetEvaluationType);
                                                    activeTargetDetails.targetProgress = targetPeriodsForMilestone[activePeriodIndex].targetValue - targetPeriodsForMilestone[activePeriodIndex].targetAchievedValue; 
                                                    activeTargetDetails.periodStatus = "UPCOMING";
                                                    break;
                                                }                                                
                                            }
                                        }
                                       

                                    }

                                }

                                distinctMileStoneItem.activeTargetDetails = activeTargetDetails;
                            }
                            #endregion

                            #region FIXED_WINDOW
                            else if (targetGroup.targetEvaluationType.Equals(Constants.MileStoneTargetTypeFixedWindow, StringComparison.OrdinalIgnoreCase))
                            {                                
                                Console.WriteLine("RequestId:{0} MileStoneItem TargetGroupId :{1} targetEvaluationType: FIXED_CALENDAR_WINDOW", requestId, distinctMileStoneItem.targetGroupId);
                                
                                targetPeriodsForMilestone.Sort((t1, t2) => t1.periodStartDate.CompareTo(t2.periodStartDate));
                                Console.WriteLine("RequestId:{0} MileStoneItem targetPeriodsForMilestone Sorted with periodStartDate asc,targetPeriodsForMilestone Count:{1}", requestId, targetPeriodsForMilestone.Count);

                                if (targetPeriodsForMilestone.Count > 0)
                                {
                                    activeTargetDetails = SetActiveTargetDetails(targetPeriodsForMilestone[0], targetGroup.targetEvaluationType);
                                    

                                    if (currentDate < Convert.ToInt32(DateTime.ParseExact(targetPeriodsForMilestone[0].periodStartDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")))
                                    {
                                        Console.WriteLine("RequestId:{0} MileStoneItem currentDate < periodStartDate :", requestId);                                        
                                        activeTargetDetails.periodStatus = "UPCOMING";
                                        distinctMileStoneItem.isExpired = false;

                                    }
                                    else if (currentDate > Convert.ToInt32(DateTime.ParseExact(targetPeriodsForMilestone[targetPeriodsForMilestone.Count - 1].periodEndDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")))
                                    {
                                        Console.WriteLine("RequestId:{0} MileStoneItem currentDate > lastPeriodEndDate :", requestId);                                        
                                        distinctMileStoneItem.isExpired = true;
                                        activeTargetDetails = null;
                                    }
                                    else
                                    {
                                        bool checkActiveTarget = false;
                                        foreach (Target target in targetPeriodsForMilestone)
                                        {
                                            if (Convert.ToInt32(DateTime.ParseExact(target.periodStartDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")) <= currentDate && currentDate <= Convert.ToInt32(DateTime.ParseExact(target.periodEndDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")))
                                            {
                                                Console.WriteLine("RequestId:{0} MileStoneItem targetValue:{1},targetAchievedValue:{2} :", requestId, target.targetValue, target.targetAchievedValue);

                                                activeTargetDetails = SetActiveTargetDetails(target, targetGroup.targetEvaluationType);
                                                checkActiveTarget = true;

                                                if (target.targetValue > target.targetAchievedValue)
                                                {
                                                    activeTargetDetails.targetProgress = target.targetValue - target.targetAchievedValue;
                                                    activeTargetDetails.periodStatus = "LOCKED";
                                                }
                                                else
                                                {
                                                    activeTargetDetails.targetProgress = 0;
                                                    activeTargetDetails.periodStatus = "UNLOCKED";
                                                }
                                                break;
                                            }
                                        }
                                        
                                        Console.WriteLine("RequestId:{0} MileStoneItem checkActiveTarget : {1} ", requestId, checkActiveTarget);
                                        if (!checkActiveTarget)  //no active targets found so checking for upcome events
                                        {
                                            Console.WriteLine("RequestId:{0} MileStoneItem no active targets found so checking for upcome events", requestId);

                                            for (int activePeriodIndex = 0; activePeriodIndex < targetPeriodsForMilestone.Count; activePeriodIndex++)
                                            {
                                                if (Convert.ToInt32(DateTime.ParseExact(targetPeriodsForMilestone[activePeriodIndex].periodEndDate, "yyyy-MM-dd", null).ToString("yyyyMMdd")) > currentDate)
                                                {
                                                    activeTargetDetails = SetActiveTargetDetails(targetPeriodsForMilestone[activePeriodIndex], targetGroup.targetEvaluationType);
                                                    activeTargetDetails.targetProgress = targetPeriodsForMilestone[activePeriodIndex].targetValue - targetPeriodsForMilestone[activePeriodIndex].targetAchievedValue;
                                                    activeTargetDetails.periodStatus = "UPCOMING";
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    distinctMileStoneItem.activeTargetDetails = activeTargetDetails;
                                    
                                }

                            }
                            #endregion

                            else
                            {
                                distinctMileStoneItem.activeTargetDetails = null;
                                Console.WriteLine("RequestId:{0} targetEvaluationType are not valid values[CYCLIC_WINDOW,FIXED_WINDOW], targetEvaluationType:{1}", requestId, targetGroup.targetEvaluationType);
                            }


                            if (activeTargetDetails != null && activeTargetDetails.periodStatus == null)  // Controlled here ,when nothing found
                                distinctMileStoneItem.activeTargetDetails = null;
                            distinctMileStoneItem.expiredInstances = null;
                        }



                    }

                }

                Console.WriteLine("{0} getCustomerPromotion Count: {1}, data Count:{2}, milestones Count:{3}", requestId, getCustomerPromotionResp.data.Count, dataBlockResponse.Count, distinctMilestonesBlockResp.Count);

                bool apiStatus = false; //Incomplete computing case so apiStatus is changed to false         
                if ((dataBlockResponse.Count > 0 || distinctMilestonesBlockResp.Count > 0 ) && (promotionGetApiDetails != null || distinctPromotionIdList.Count == 0) && getTargetDetailsResp != null)
                    apiStatus = true;

                var ShellResponse = new MileStoneOffersEndRespose
                {
                    status = apiStatus,
                    data = dataBlockResponse,
                    milestones = distinctMilestonesBlockResp,
                };

                Console.WriteLine("{0} {1} ", requestId, JsonConvert.SerializeObject(ShellResponse));
                return JsonConvert.SerializeObject(ShellResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine("RequestId:{0}.Exception encountered in Mapper.Map().Message:'{1}'", requestId, e.Message);
            }
            return string.Empty;
        }

        internal static GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Availableinstance SetAvailableInstances(GEN_Shell_MobileBackend_API.Models.Promotions.Datum responseItem)
        {
            try
            {
                //Card,Earn Extraction
                List<Models.MileStoneOffersResp.Cart> cartList = new List<Models.MileStoneOffersResp.Cart>();
                List<Models.MileStoneOffersResp.Earn> earnList = new List<Models.MileStoneOffersResp.Earn>();
                if (responseItem != null && responseItem.restrictions.Cart != null && responseItem.restrictions.Cart.Count > 0)
                {
                    responseItem.restrictions.Cart.Select(responseItemCustomerCart =>
                    {
                        var newCartToMileStoneResp = new Models.MileStoneOffersResp.Cart
                        {
                            kpi = responseItemCustomerCart.kpi,
                            maxLimit = responseItemCustomerCart.maxLimit,
                            remainingRedemption = responseItemCustomerCart.remainingRedemption,
                        };

                        cartList.Add(newCartToMileStoneResp);
                        return cartList;
                    }).ToList();
                }

                if (responseItem != null && responseItem.restrictions.Earn != null && responseItem.restrictions.Earn.Count > 0)
                {
                    responseItem.restrictions.Earn.Select(responseItemcustomerEarn =>
                    {
                        var newEarnToMileStoneResp = new Models.MileStoneOffersResp.Earn
                        {
                            kpi = responseItemcustomerEarn.kpi,
                            maxLimit = responseItemcustomerEarn.maxLimit,
                            remainingRedemption = responseItemcustomerEarn.remainingRedemption,
                        };

                        earnList.Add(newEarnToMileStoneResp);
                        return earnList;
                    }).ToList();
                }

                var availableInstance = new GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Availableinstance()
                {
                    earnedPromotionId = responseItem.earnedPromotionId,
                    promotionId = responseItem.promotionId,
                    promotionName = responseItem.promotionName,
                    validTill = responseItem.validTill,
                    unlockedDate = responseItem.unlockedDate,
                    customerId = responseItem.customerId,
                    earnedType = responseItem.earnedType,
                    earnedStatus = responseItem.earnedStatus,
                    promotionStatus = responseItem.promotionStatus,
                    mileStoneId = responseItem.mileStoneId,
                    targetGroupId = responseItem.targetGroupId,
                    applicationMode = responseItem.applicationMode,
                    redeemableFrom = responseItem.redeemableFrom,
                    customFieldValues = responseItem.customFieldValues,
                    restrictions = new Models.MileStoneOffersResp.Restrictions()
                    {
                        Cart = cartList,
                        Earn = earnList
                    },
                    eventTime = responseItem.eventTime,
                };

                return availableInstance;
            }
            catch (Exception e)
            {
                Console.WriteLine("PromotionId:{0}.Exception encountered in SetAvailableInstances().Message:'{1}'", responseItem.promotionId, e.Message);
            }
            return default(GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Availableinstance);

        }

        internal static GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Expiredinstance SetExpiredInstances(GEN_Shell_MobileBackend_API.Models.Promotions.Datum responseItem)
        {
            try
            {
                List<Models.MileStoneOffersResp.Cart> cartList = new List<Models.MileStoneOffersResp.Cart>();
                List<Models.MileStoneOffersResp.Earn> earnList = new List<Models.MileStoneOffersResp.Earn>();

                if (responseItem != null && responseItem.restrictions.Cart != null && responseItem.restrictions.Cart.Count > 0)
                {
                    responseItem.restrictions.Cart.Select(responseItemCustomerCart =>
                    {
                        var newCartToMileStoneResp = new Models.MileStoneOffersResp.Cart
                        {
                            kpi = responseItemCustomerCart.kpi,
                            maxLimit = responseItemCustomerCart.maxLimit,
                            remainingRedemption = responseItemCustomerCart.remainingRedemption,
                        };

                        cartList.Add(newCartToMileStoneResp);
                        return cartList;
                    }).ToList();
                }
                if (responseItem != null && responseItem.restrictions.Earn != null && responseItem.restrictions.Earn.Count > 0)
                {
                    responseItem.restrictions.Earn.Select(responseItemCustomerEarn =>
                    {
                        var newEarnToMileStoneResp = new Models.MileStoneOffersResp.Earn
                        {
                            kpi = responseItemCustomerEarn.kpi,
                            maxLimit = responseItemCustomerEarn.maxLimit,
                            remainingRedemption = responseItemCustomerEarn.remainingRedemption,
                        };

                        earnList.Add(newEarnToMileStoneResp);
                        return earnList;
                    }).ToList();
                }

                var availableInstance = new GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Expiredinstance()
                {
                    earnedPromotionId = responseItem.earnedPromotionId,
                    promotionId = responseItem.promotionId,
                    promotionName = responseItem.promotionName,
                    validTill = responseItem.validTill,
                    unlockedDate = responseItem.unlockedDate,
                    customerId = responseItem.customerId,
                    earnedType = responseItem.earnedType,
                    earnedStatus = responseItem.earnedStatus,
                    promotionStatus = responseItem.promotionStatus,
                    mileStoneId = responseItem.mileStoneId,
                    targetGroupId = responseItem.targetGroupId,
                    applicationMode = responseItem.applicationMode,
                    redeemableFrom = responseItem.redeemableFrom,
                    customFieldValues = responseItem.customFieldValues,
                    restrictions = new Models.MileStoneOffersResp.Restrictions()
                    {
                        Cart = cartList,
                        Earn = earnList
                    },
                    eventTime = responseItem.eventTime,
                };

                return availableInstance;
            }
            catch (Exception e)
            {
                Console.WriteLine("PromotionId:{0}.Exception encountered in SetExpiredInstances().Message:'{1}'", responseItem.promotionId, e.Message);
            }

            return default(GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Expiredinstance);

        }

        internal static GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Datum SetDatum(GEN_Shell_MobileBackend_API.Models.Promotions.Datum responseItem)
        {
            try
            {
                List<Models.MileStoneOffersResp.Cart1> cartList = new List<Models.MileStoneOffersResp.Cart1>();
                List<Models.MileStoneOffersResp.Customer1> customer1List = new List<Models.MileStoneOffersResp.Customer1>();
                if (responseItem != null && responseItem.restrictions.Cart != null && responseItem.restrictions.Cart.Count > 0)
                {
                    responseItem.restrictions.Cart.Select(responseItemCustomerCart =>
                    {
                        var newCartToMileStoneResp = new Models.MileStoneOffersResp.Cart1
                        {
                            kpi = responseItemCustomerCart.kpi,
                            maxLimit = responseItemCustomerCart.maxLimit,
                            remainingRedemption = responseItemCustomerCart.remainingRedemption,
                        };

                        cartList.Add(newCartToMileStoneResp);
                        return cartList;
                    }).ToList();
                }


                if (responseItem != null && responseItem.restrictions.Customer != null && responseItem.restrictions.Customer.Count > 0)
                {
                    responseItem.restrictions.Customer.Select(responseItemCustomerEarn =>
                    {
                        var newCustomer1ToMileStoneResp = new Models.MileStoneOffersResp.Customer1
                        {
                            kpi = responseItemCustomerEarn.kpi,
                            maxLimit = responseItemCustomerEarn.maxLimit,
                            remainingRedemption = responseItemCustomerEarn.remainingRedemption,
                        };

                        customer1List.Add(newCustomer1ToMileStoneResp);
                        return customer1List;
                    }).ToList();
                }


                var instance = new GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Datum()
                {
                    earnedPromotionId = responseItem.earnedPromotionId,
                    promotionId = responseItem.promotionId,
                    promotionName = responseItem.promotionName,
                    validTill = responseItem.validTill,
                    unlockedDate = responseItem.unlockedDate,
                    customerId = responseItem.customerId,
                    earnedType = responseItem.earnedType,
                    earnedStatus = responseItem.earnedStatus,
                    promotionStatus = responseItem.promotionStatus,
                    applicationMode = responseItem.applicationMode,
                    redeemableFrom = responseItem.redeemableFrom,
                    customFieldValues = responseItem.customFieldValues,
                    restrictions = new Models.MileStoneOffersResp.Restrictions1()
                    {
                        Cart = cartList,
                        Customer = customer1List
                    },
                    eventTime = responseItem.eventTime,
                };
                return instance;
            }
            catch (Exception e)
            {
                Console.WriteLine("PromotionId:{0}.Exception encountered in SetDatum().Message:'{1}'", responseItem.promotionId, e.Message);
            }
            return default(GEN_Shell_MobileBackend_API.Models.MileStoneOffersResp.Datum);

        }

        internal static Activetargetdetails SetActiveTargetDetails(Target target, string targetEvaluationType)
        {
            var activeTargetInstance = new Activetargetdetails()
            {
                targetId = target.targetId,
                targetEvaluationType = targetEvaluationType,
                periodId = target.periodId,
                periodRefCode = target.periodRefCode,
                periodStartDate = target.periodStartDate,
                periodEndDate = target.periodEndDate,
                targetValue = target.targetValue,
                targetAchievedValue = target.targetAchievedValue,
                targetName = target.targetName,
                targetType = target.targetType,
                targetEntity = target.targetEntity,
                targetRuleId = target.targetRuleId,
                currentPeriod = target.currentPeriod,
                milestones = target.milestones,
            };
            return activeTargetInstance;
        }
        public CustomerAddRequest Map(string surveyFlag, DateTime surveyDate, double surveyIntervalDays ){
            if (surveyFlag != null && "FALSE".Equals(surveyFlag, StringComparison.OrdinalIgnoreCase))
            {
                var timeDifference = (DateTime.Today - surveyDate).TotalDays;
                if (timeDifference >= surveyIntervalDays) surveyFlag = "true";

            }
            else
            {
                surveyFlag = "true";
            }
            IDictionary<string, string> updatedFields = new Dictionary<string, string>();

            //updatedFields.Add(Constants.lastTxnDate, DateTime.Now.ToString("yyyy-MM-dd"));
            updatedFields.Add(Constants.LastTxnDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            if (surveyFlag.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                updatedFields.Add(Constants.surveyFlag, "true");

            }
            Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile updatedProfile = new Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile
            {
                fields = updatedFields
            };
            List<Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile> updatedProfiles = new List<Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile>();
            updatedProfiles.Add(updatedProfile);
            CustomerAddRequest customerAddRequest = new CustomerAddRequest
            {
                profiles = updatedProfiles
            };
            return customerAddRequest;
        }
        public CustomerAddRequest Map(){
            IDictionary<string, string> updatedFields = new Dictionary<string, string>();
            //updatedFields.Add(Constants.LastTxnDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            updatedFields.Add(Constants.RewardDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile updatedProfile = new Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile
            {
                fields = updatedFields
            };
            List<Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile> updatedProfiles = new List<Capillary.ShellProxy.Model.CustomerAddModel.Request.Profile>();
            updatedProfiles.Add(updatedProfile);
            CustomerAddRequest customerAddRequest = new CustomerAddRequest
            {
                profiles = updatedProfiles
            };
            return customerAddRequest;
        }
         public VocBehavioralEventRequest Map(String Profile_identifier,String eventName){
            VocBehavioralEventRequest vocBehavioralEventRequest = new VocBehavioralEventRequest
            {
                mobile = Profile_identifier,
                event_name = eventName,
                reward = Constants.reward
            };
            return vocBehavioralEventRequest;
        }
        public SmgSurveyRequest Map(SmgSurveyRequest smgSurveyRequest){
            var storeIdValObj = smgSurveyRequest.values.Where(c => c.key  == "storeId").Select(x => x.valueObject.value).FirstOrDefault();
            if(storeIdValObj ==null)return smgSurveyRequest;
            string storeIdVal = storeIdValObj.ToString();
            if(!string.IsNullOrEmpty(JsonConvert.SerializeObject(storeIdVal)) && !storeIdVal.ToLower().Contains("merchant")){
                string[] storeCodeVals = storeIdVal.Split('.',StringSplitOptions.None);
                string newStoreIdVal = storeCodeVals[storeCodeVals.Length-1];
                foreach(var value in smgSurveyRequest.values){
                    if(value.key.Equals("storeId",StringComparison.OrdinalIgnoreCase)){
                        value.valueObject.value = newStoreIdVal;
                        //return smgSurveyRequest;
                        break;
                    }
                }
            }
            var locationIdVal = smgSurveyRequest.locationId;
            if(!string.IsNullOrEmpty(locationIdVal) && !locationIdVal.ToLower().Contains("merchant")){
                string[] locationCodeVals = locationIdVal.Split('.',StringSplitOptions.None);
                string newLocationIdVal = locationCodeVals[locationCodeVals.Length-1];
                smgSurveyRequest.locationId = newLocationIdVal;
            }
            return smgSurveyRequest;
        }

    }
}

