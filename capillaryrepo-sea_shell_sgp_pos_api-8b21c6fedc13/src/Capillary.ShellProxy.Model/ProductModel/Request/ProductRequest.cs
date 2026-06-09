using System;
using System.Collections.Generic;
using System.Text;
namespace Capillary.ShellProxy.Model.ProductModel.Request
{
    public class Product
    {
        public string sku { get; set; }
        public string variantsku { get; set; }
        public string stock { get; set; }
        public string locationrefcode { get; set; }
        public string Quantity { get; set; }
        public string MRP { get; set; }
        public string WebPrice { get; set; }
        public string TokenPrice { get; set; }
    }

    public class Products
    {
        public Products()
        {
            product = new List<Product>();
        }
        public List<Product> product { get; set; }
    }

    public class ProductRequest
    {
        public ProductRequest()
        {
            products = new Products();
        }
        public Products products { get; set; }
    }

}