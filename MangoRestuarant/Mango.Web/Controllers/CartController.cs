using Mango.Web.Models;
using Mango.Web.Services.IServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mango.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICartService _cartService;
        private readonly ICouponService _couponService;
        public CartController(IProductService productService, ICartService cartService, ICouponService couponService)
        {
            _productService = productService;
            _couponService = couponService;
            _cartService = cartService;
        }
        public async Task<IActionResult> CartIndex()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        [HttpPost]
        [ActionName("ApplyCoupon")]
        public async Task<IActionResult> ApplyCoupon(CartDto cartDto)
        {
            var userId = "1"; // User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = "";  //await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.ApplyCoupon<ResponseDto>(cartDto, accessToken);

            if (response != null && response.IsSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }
            return View();
        }

        [HttpPost]
        [ActionName("RemoveCoupon")]
        public async Task<IActionResult> RemoveCoupon(CartDto cartDto)
        {
            var userId = "1"; // User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = "";  //await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.RemoveCoupon<ResponseDto>(cartDto.CartHeader.UserId, accessToken);

            if (response != null && response.IsSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }
            return View();
        }

        public async Task<IActionResult> Remove(int cartDetailsId)
        {
            var userId = 1.ToString(); //User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = "";  //await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.RemoveFromCartAsync<ResponseDto>(cartDetailsId, accessToken);

            
            if (response != null && response.IsSuccess)
            {
                return RedirectToAction(nameof(CartIndex));
            }
            return View();
        }

      
        public async Task<IActionResult> Checkout()
        {
            return View(await LoadCartDtoBasedOnLoggedInUser());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CartDto cartDto) 
        {
            try
            {
                var model  =  new CheckoutHeaderDto();
                model.CVV = cartDto.CartHeader.CVV;
                model.CartDetails = cartDto.CartDetails;
                model.FirstName = cartDto.CartHeader.FirstName;
                model.LastName = cartDto.CartHeader.LastName;
                model.ExpiryMonthYear = cartDto.CartHeader.ExpiryMonthYear;
                model.OrderTotal = cartDto.CartHeader.OrderTotal;
                model.CartHeaderId = cartDto.CartHeader.CartHeaderId;
                model.CouponCode = cartDto.CartHeader.CouponCode;
                model.CardNumber = cartDto.CartHeader.CardNumber;
                model.Email = cartDto.CartHeader.Email;
                model.Phone = cartDto.CartHeader.Phone;
                model.PickupDateTime = cartDto.CartHeader.PickupDateTime;
                model.DiscountTotal = cartDto.CartHeader.DiscountTotal;
                model.UserId = cartDto.CartHeader.UserId;

                var accessToken = "";  //await HttpContext.GetTokenAsync("access_token");
                var response = await _cartService.Checkout<ResponseDto>(model, accessToken);
                if (!response.IsSuccess)
                {
                    TempData["Error"] = response.DisplayMessage;
                    return RedirectToAction(nameof(Checkout));
                }
                return RedirectToAction(nameof(Confirmation));
            }
            catch(Exception e)
            {
                return View(cartDto);
            }
        }
       
        public async Task<IActionResult> Confirmation()
        {
            return View();
        }
        private async Task<CartDto> LoadCartDtoBasedOnLoggedInUser()
        {
            var userId = 1.ToString(); //User.Claims.Where(u => u.Type == "sub")?.FirstOrDefault()?.Value;
            var accessToken = "";// await HttpContext.GetTokenAsync("access_token");
            var response = await _cartService.GetCartByUserIdAsnyc<ResponseDto>(userId, accessToken);

            CartDto cartDto = new();
            if(response!=null && response.IsSuccess)
            {
                cartDto = JsonConvert.DeserializeObject<CartDto>(Convert.ToString(response.Result));
            }

            if (cartDto.CartHeader != null)
            {
                if (!string.IsNullOrEmpty(cartDto.CartHeader.CouponCode))
                {
                    var coupon = await _couponService.GetCoupon<ResponseDto>(cartDto.CartHeader.CouponCode, accessToken);
                    if (coupon != null && coupon.IsSuccess)
                    {
                        var couponObj = JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(coupon.Result));
                        cartDto.CartHeader.DiscountTotal = couponObj.DiscountAmount;
                    }
                }

                foreach (var detail in cartDto.CartDetails) {
                    cartDto.CartHeader.OrderTotal += (detail.Product.Price * detail.Count);
                }

                cartDto.CartHeader.OrderTotal -= cartDto.CartHeader.DiscountTotal;
            }
            return cartDto;
        }
    }
}
