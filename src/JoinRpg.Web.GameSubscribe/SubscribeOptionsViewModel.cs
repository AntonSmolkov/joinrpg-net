using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Web.GameSubscribe;

public class SubscribeOptionsViewModel : ISubscriptionOptions
{

    [Display(
        Name = "Подписка на новые заявки/прием/отклонение",
        Description = "Будут приходить уведомления о любых изменениях статуса заявки")]
    public bool ClaimStatusChange { get; set; }

    [Display(Name = "Подписка на комментарии", Description = "Будут приходить уведомления о любых комментариях к заявке")]
    public bool Comments { get; set; }

    [Display(Name = "Подписка на изменение полей персонажа/заявки")]
    public bool FieldChange { get; set; }

    [Display(Name = "Подписка на финансовые операции", Description = "Будут приходить уведомления о сданном взносе, его изменении и других финансовых операциях")]
    public bool MoneyOperation { get; set; }

    [Display(Name = "Подписка на операции с поселением", Description = "Будут приходить уведомления о изменении типа поселения, назначении комнаты и других операциях с поселением")]
    public bool AccommodationChange { get; set; }

    public bool AllSet => ClaimStatusChange && Comments && FieldChange && MoneyOperation && AccommodationChange;
}
