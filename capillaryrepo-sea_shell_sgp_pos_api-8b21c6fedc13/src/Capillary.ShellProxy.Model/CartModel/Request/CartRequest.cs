using System;
using System.Collections.Generic;
using System.Text;

namespace Capillary.ShellProxy.Model.CartModel.Request
{
    public class CartRequest
    {
        public Cart cart { get; set; }
    }
    public class Item
    {
        public string Status { get; set; }
        public string LocationId { get; set; }
        public string Quantity { get; set; }
        public string ProductID { get; set; }
        public int VariantProductId { get; set; }
        public string CartReferenceKey { get; set; }
    }

    public class Cart
    {
        public string delveryMode { get; set; }
        public List<Item> Item { get; set; }
    }

}
