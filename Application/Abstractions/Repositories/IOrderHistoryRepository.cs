using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOrderHistoryRepository
{
    Task<long> AddAsync(OrderHistory orderHistory);
    Task<IEnumerable<OrderHistory>> SearchAsync(IEnumerable<long> orderIds, IEnumerable<OrderHistoryItemKind> historyKinds);
}
