using Spice.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Utility
{
    public static class SD
    {
        public const string DefaultFoodImage = "default_food.png";
        public const string ManagerUser = "ManagerUser";
        public const string CustomEndUser = "CustomEndUser";
        public const string FrontDeskUser = "FrontDeskUser";
        public const string KitchenUser = "KitchenUser";
        public const string ssShoppingCartCount = "ssCartCount";
        public const string ssCouponCode = "ssCouponCode";
        //Order Status
        public const string OrderStatusSubmitted = "Submitted";
        public const string OrderStatusInProcess = "InProcess";
        public const string OrderStatusReady = "Ready";
        public const string OrderStatusCompleted = "Completed";
        public const string OrderStatusCancelled = "Cancelled";
        //Payment Status
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApprove = "Approve";
        public const string PaymentStatusRejected = "Rejected";

        public static string ConvertToRawHtml(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for(int i=0; i< source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array,0,arrayIndex);
        }

        //Calculate the discounted order price on the basis of coupon code
        public static double DiscountedPrice(Coupon couponFromDB, double OriginalOrderTotal)
        {
            //check the coupon is null
            if (couponFromDB==null)
            {
                return OriginalOrderTotal;
            }
            else
            {
                if (couponFromDB.MinimumAmount > OriginalOrderTotal)
                {
                    return OriginalOrderTotal;
                }
                else
                {
                    //check the coupon type and calculate the discount amount
                    //calculate on the basis of dollor amount
                    if (Convert.ToInt32(couponFromDB.CouponType) == (int)Coupon.ECouponType.Dollar)
                    {
                        return Math.Round(OriginalOrderTotal - couponFromDB.Discount,2);
                    }
                    if (Convert.ToInt32(couponFromDB.CouponType) == (int)Coupon.ECouponType.Percent)
                    {
                        double pctAmt = (OriginalOrderTotal * couponFromDB.Discount) / 100;
                        return Math.Round(OriginalOrderTotal - pctAmt, 2);
                    }
                }
            }
            return OriginalOrderTotal;
        }
    }
}
