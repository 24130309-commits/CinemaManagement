using Xunit;
using System;
using System.Collections.Generic;
using CinemaManagement.BLL.Services;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services
{
    public class PromotionServiceTests
    {
        [Fact]
        public void ValidateAndGetPromotion_WhenExpired_ThrowsBusinessException()
        {
            var repo = new StubPromotionRepository
            {
                PromoToReturn = new Promotion
                {
                    Code = "OLD20",
                    DiscountPercent = 20,
                    StartDate = DateTime.Now.AddDays(-10),
                    EndDate = DateTime.Now.AddDays(-1),
                    IsActive = true
                }
            };
            var service = new PromotionService(repo);

            try
            {
                service.ValidateAndGetPromotion("OLD20");
                throw new Exception("Expected BusinessException was not thrown.");
            }
            catch (BusinessException)
            {
                // Pass
            }
        }

        private class StubPromotionRepository : IPromotionRepository
        {
            public Promotion PromoToReturn { get; set; }
            public Promotion GetByCode(string code) => PromoToReturn;
            public bool IsCodeExists(string code, int? excludeId = null) => false;
            public IEnumerable<Promotion> GetAll() => new List<Promotion>();
            public Promotion GetById(int id) => PromoToReturn;
            public void Add(Promotion entity) { }
            public void Update(Promotion entity) { }
            public void Delete(int id) { }
        }
    }
}