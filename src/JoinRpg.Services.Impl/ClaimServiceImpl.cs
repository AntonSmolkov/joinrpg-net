using System.Data.Entity.Validation;
using System.Diagnostics;
using JetBrains.Annotations;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.CharacterFields;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl;

[UsedImplicitly]
internal class ClaimServiceImpl : ClaimImplBase, IClaimService
{

    public async Task SubscribeClaimToUser(int projectId, int claimId)
    {
        var user = await UserRepository.GetWithSubscribe(CurrentUserId);
        _ = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(CurrentUserId);
        _ = user.Subscriptions.Add(
            new UserSubscription() { ClaimId = claimId, ProjectId = projectId }.AssignFrom(
                SubscribeOptionsExtensions.AllSet()));
        await UnitOfWork.SaveChangesAsync();
    }

    public async Task UnsubscribeClaimToUser(int projectId, int claimId)
    {
        var user = await UserRepository.GetWithSubscribe(CurrentUserId);
        _ = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(CurrentUserId);
        var subscription = user.Subscriptions.FirstOrDefault(s =>
            s.ProjectId == projectId && s.UserId == CurrentUserId && s.ClaimId == claimId);
        if (subscription != null)
        {
            _ = UnitOfWork.GetDbSet<UserSubscription>().Remove(subscription);
            await UnitOfWork.SaveChangesAsync();
        }
    }

    public async Task CheckInClaim(int projectId, int claimId, int money)
    {
        var claim = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(CurrentUserId); //TODO Specific right
        claim.EnsureCanChangeStatus(Claim.Status.CheckedIn);

        var validator = new ClaimCheckInValidator(claim);
        if (!validator.CanCheckInInPrinciple)
        {
            throw new ClaimWrongStatusException(claim);
        }

        FinanceOperationEmail? financeEmail = null;
        if (money > 0)
        {
            var paymentType = claim.Project.GetCashPaymentType(CurrentUserId);

            if (paymentType == null)
            {
                throw new JoinRpgInvalidUserException();
            }

            financeEmail = await AcceptFeeImpl(".", Now, 0, money, paymentType, claim);
        }
        else if (money < 0)
        {
            throw new InvalidOperationException();
        }

        if (!validator.CanCheckInNow)
        {
            throw new ClaimWrongStatusException(claim);
        }

        claim.ClaimStatus = Claim.Status.CheckedIn;
        claim.CheckInDate = Now;
        Debug.Assert(claim.Character != null, "claim.Character != null");
        MarkChanged(claim.Character);
        claim.Character.InGame = true;

        _ = AddCommentImpl(claim, null, ".", true, CommentExtraAction.CheckedIn);

        await UnitOfWork.SaveChangesAsync();

        await
          EmailService.Email(
            await CreateClaimEmail<CheckedInEmal>(claim, ".", s => s.ClaimStatusChange,
              CommentExtraAction.ApproveByMaster));

        if (financeEmail != null)
        {
            await EmailService.Email(financeEmail);
        }
    }

    public async Task<int> MoveToSecondRole(int projectId, int claimId, int characterId)
    {
        var oldClaim = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(CurrentUserId); //TODO Specific right
        oldClaim.EnsureStatus(Claim.Status.CheckedIn);

        Debug.Assert(oldClaim.Character != null, "oldClaim.Character != null");
        oldClaim.Character.InGame = false;
        MarkChanged(oldClaim.Character);

        var source = await CharactersRepository.GetCharacterAsync(projectId, characterId);

        MarkChanged(source);

        // TODO improve valitdation here
        //source.EnsureCanMoveClaim(oldClaim);

        var responsibleMaster = source.GetResponsibleMaster();
        var claim = new Claim()
        {
            CharacterGroupId = null,
            CharacterId = characterId,
            ProjectId = projectId,
            PlayerUserId = oldClaim.PlayerUserId,
            PlayerAcceptedDate = Now,
            CreateDate = Now,
            ClaimStatus = Claim.Status.Approved,
            CurrentFee = 0,
            ResponsibleMasterUserId = responsibleMaster.UserId,
            ResponsibleMasterUser = responsibleMaster,
            LastUpdateDateTime = Now,
            MasterAcceptedDate = Now,
            CommentDiscussion =
            new CommentDiscussion() { CommentDiscussionId = -1, ProjectId = projectId },
        };

        claim.CommentDiscussion.Comments.Add(new Comment
        {
            CommentDiscussionId = -1,
            AuthorUserId = CurrentUserId,
            CommentText = new CommentText { Text = new MarkdownString(".") },
            CreatedAt = Now,
            IsCommentByPlayer = false,
            IsVisibleToPlayer = true,
            ProjectId = projectId,
            LastEditTime = Now,
            ExtraAction = CommentExtraAction.SecondRole,
        });

        oldClaim.ClaimStatus = Claim.Status.Approved;
        source.ApprovedClaim = claim;
        _ = AddCommentImpl(oldClaim, null, ".", true, CommentExtraAction.OutOfGame);


        _ = UnitOfWork.GetDbSet<Claim>().Add(claim);

        // ReSharper disable once UnusedVariable TODO decide if we should send email if FieldDefaultValueGenerator changes something
        var updatedFields =
          FieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, new Dictionary<int, string?>(),
            FieldDefaultValueGenerator);

        await UnitOfWork.SaveChangesAsync();

        var claimEmail = await CreateClaimEmail<SecondRoleEmail>(claim, "",
            s => s.ClaimStatusChange,
            CommentExtraAction.NewClaim);

        await EmailService.Email(claimEmail);

        return claim.ClaimId;
    }

    public async Task AddClaimFromUser(int projectId,
        int? characterGroupId,
        int? characterId,
        string claimText,
        IReadOnlyDictionary<int, string?> fields)
    {
        var source = await ProjectRepository.GetClaimSource(projectId, characterGroupId, characterId);

        source.EnsureCanAddClaim(CurrentUserId);

        User responsibleMaster = source.GetResponsibleMaster();

        var claim = new Claim()
        {
            CharacterGroupId = characterGroupId,
            CharacterId = characterId,
            ProjectId = projectId,
            PlayerUserId = CurrentUserId,
            PlayerAcceptedDate = Now,
            CreateDate = Now,
            ClaimStatus = Claim.Status.AddedByUser,
            ResponsibleMasterUserId = responsibleMaster.UserId,
            ResponsibleMasterUser = responsibleMaster,
            LastUpdateDateTime = Now,
            CommentDiscussion = new CommentDiscussion() { CommentDiscussionId = -1, ProjectId = projectId },
        };

        if (!string.IsNullOrWhiteSpace(claimText))
        {
            claim.CommentDiscussion.Comments.Add(new Comment
            {
                CommentDiscussionId = -1,
                AuthorUserId = CurrentUserId,
                CommentText = new CommentText { Text = new MarkdownString(claimText) },
                CreatedAt = Now,
                IsCommentByPlayer = true,
                IsVisibleToPlayer = true,
                ProjectId = projectId,
                LastEditTime = Now,
                ExtraAction = CommentExtraAction.NewClaim,
            });
        }

        _ = UnitOfWork.GetDbSet<Claim>().Add(claim);

        var updatedFields = FieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, fields, FieldDefaultValueGenerator);

        var claimEmail = await CreateClaimEmail<NewClaimEmail>(claim, claimText ?? "", s => s.ClaimStatusChange,
          CommentExtraAction.NewClaim);

        claimEmail.UpdatedFields = updatedFields;

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(claimEmail);

        if (claim.Project.Details.AutoAcceptClaims)
        {
            var userId = claim.ResponsibleMasterUserId;
            StartImpersonate(userId);
            //TODO[Localize]
            await ApproveByMaster(projectId,
                claim.ClaimId,
                "Ваша заявка была принята автоматически");
            ResetImpersonation();
        }
    }

    public async Task AddComment(int projectId, int claimId, int? parentCommentId, bool isVisibleToPlayer, string commentText, FinanceOperationAction financeAction)
    {
        var claim = (await ClaimsRepository.GetClaim(projectId, claimId)).RequestAccess(CurrentUserId,
            ExtraAccessReason.Player);

        SetDiscussed(claim, isVisibleToPlayer);

        var parentComment = claim.CommentDiscussion.Comments.SingleOrDefault(c => c.CommentId == parentCommentId);

        Func<UserSubscription, bool> predicate = s => s.Comments;
        CommentExtraAction? extraAction = null;

        if (financeAction != FinanceOperationAction.None)
        {
            if (parentComment is null)
            {
                throw new InvalidOperationException("Requested to perform finance operation on parent comment, but there is no any");
            }
            extraAction = PerformFinanceOperation(financeAction, parentComment, claim);
            predicate = s => s.Comments || s.MoneyOperation;
        }


        var email = await AddCommentWithEmail<AddCommentEmail>(commentText, claim, isVisibleToPlayer,
          predicate, parentComment, extraAction);

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(email);
    }

    private CommentExtraAction? PerformFinanceOperation(FinanceOperationAction financeAction,
      Comment parentComment, Claim claim)
    {
        var finance = parentComment?.Finance;
        if (finance == null)
        {
            throw new InvalidOperationException();
        }

        if (!finance.RequireModeration)
        {
            throw new ValueAlreadySetException("Finance entry is already moderated.");
        }

        finance.RequestModerationAccess(CurrentUserId);
        finance.Changed = Now;
        switch (financeAction)
        {
            case FinanceOperationAction.Approve:
                finance.State = FinanceOperationState.Approved;
                if (finance.OperationType == FinanceOperationType.PreferentialFeeRequest)
                {
                    claim.PreferentialFeeUser = true;
                }
                claim.UpdateClaimFeeIfRequired(finance.OperationDate);
                return CommentExtraAction.ApproveFinance;
            case FinanceOperationAction.Decline:
                finance.State = FinanceOperationState.Declined;
                return CommentExtraAction.RejectFinance;
            case FinanceOperationAction.None:
            default:
                throw new ArgumentOutOfRangeException(nameof(financeAction), financeAction, null);
        }
    }

    public async Task ApproveByMaster(int projectId, int claimId, string commentText)
    {
        var claim = await LoadClaimForApprovalDecline(projectId, claimId, CurrentUserId);

        if (claim.ClaimStatus == Claim.Status.CheckedIn)
        {
            throw new ClaimWrongStatusException(claim);
        }

        commentText ??= "";

        if (claim.Character?.CharacterType == CharacterType.Slot)
        {
            var character = CreateCharacterFromSlot(claim.Character, claim.Player);
            claim.Character = character;
            claim.CharacterId = character.CharacterId;
        }

        else if (claim.Group != null)
        {
            ConvertToIndividual(claim);
        }

        claim.MasterAcceptedDate = Now;
        claim.ChangeStatusWithCheck(Claim.Status.Approved);

        _ = AddCommentImpl(claim, null, commentText, true, CommentExtraAction.ApproveByMaster);

        if (!claim.Project.Details.EnableManyCharacters)
        {
            foreach (var otherClaim in claim.OtherPendingClaimsForThisPlayer())
            {
                otherClaim.EnsureCanChangeStatus(Claim.Status.DeclinedByMaster);
                otherClaim.MasterDeclinedDate = Now;
                otherClaim.ClaimStatus = Claim.Status.DeclinedByMaster;
                await
                    EmailService.Email(
                        await
                            AddCommentWithEmail<DeclineByMasterEmail>(
                                "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект",
                                otherClaim, true, s => s.ClaimStatusChange, null,
                                CommentExtraAction.DeclineByMaster));
            }
        }

        MarkCharacterChangedIfApproved(claim);
        Debug.Assert(claim.Character != null, "claim.Character != null");
        claim.Character.ApprovedClaimId = claim.ClaimId;
        claim.Character.IsHot = false;

        //We need to re-save fields here. Reasons:
        // 1. If we created character during approving, we need to set name for character
        // 2. M.b. we need to move some field values from Claim to Characters
        // 3. (2) Could activate changing of special groups
        // ReSharper disable once MustUseReturnValue we don't need send email here
        _ = FieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, new Dictionary<int, string?>(),
            FieldDefaultValueGenerator);

        await UnitOfWork.SaveChangesAsync();

        await
            EmailService.Email(
               await CreateClaimEmail<ApproveByMasterEmail>(claim, commentText,
                    s => s.ClaimStatusChange,
                    CommentExtraAction.ApproveByMaster));
    }

    private static Character CreateCharacterFromSlot(Character slot, User player)
    {

        switch (slot.CharacterSlotLimit)
        {
            case null:  // Unlimited slot
                break;
            case > 0:
                slot.CharacterSlotLimit--;
                break;
            default:
                throw new JoinRpgSlotLimitedException(slot);
        }


        if (slot.CharacterType != CharacterType.Slot)
        {
            throw new EntityWrongStatusException(slot);
        }

        var newCharacter = new Character()
        {
            ApprovedClaim = null,
            ApprovedClaimId = null,
            AutoCreated = true,
            OriginalCharacterSlot = slot,
            CanBePermanentlyDeleted = false,
            CharacterId = -1,
            CharacterName = slot.CharacterName, // Probably will be updated by field save
            CharacterSlotLimit = null,
            CharacterType = CharacterType.Player,
            CreatedAt = DateTime.Now,
            CreatedBy = player,
            CreatedById = player.UserId,
            DirectlyRelatedPlotElements = slot.DirectlyRelatedPlotElements,
            HidePlayerForCharacter = slot.HidePlayerForCharacter,
            InGame = false,
            IsAcceptingClaims = true,
            IsActive = true,
            IsHot = false,
            IsPublic = slot.IsPublic,
            JsonData = slot.JsonData,
            ParentCharacterGroupIds = slot.ParentCharacterGroupIds,
            PlotElementOrderData = slot.PlotElementOrderData,
            Project = slot.Project,
            ProjectId = slot.ProjectId,
            Subscriptions = slot.Subscriptions,
            UpdatedAt = DateTime.Now,
            UpdatedBy = player,
            UpdatedById = player.UserId,
        };

        return newCharacter;
    }


    private void ConvertToIndividual(Claim claim)
    {
        if (claim.Group == null)
        {
            throw new InvalidOperationException();
        }

        if (claim.Group.AvaiableDirectSlots > 0)
        {
            claim.Group.AvaiableDirectSlots -= 1;
        }

        var character = new Character()
        {
            CharacterName = "$$$ERROR", // always be overwritten later, just for be sure
            Project = claim.Project,
            ProjectId = claim.ProjectId,
            IsAcceptingClaims = true,
            IsPublic = claim.Group.IsPublic,
            IsActive = true,
            ParentCharacterGroupIds = new[] { claim.Group.CharacterGroupId },
            CharacterId = -1,
            AutoCreated = true,
        };
        MarkCreatedNow(character);
        claim.CharacterGroupId = null;
        claim.Character = character;
        claim.CharacterId = character.CharacterId;

        if (claim.Project.Details.CharacterNameLegacyMode)
        {
            //TODO[Localize]
            //Actually, legacy mode will be probably removed before localization
            character.CharacterName = $"Новый персонаж в группе {claim.Group.CharacterGroupName}";
        }
        // if not Legacy mode, name will be set on re-saving field later
    }


    public async Task DeclineByMaster(int projectId, int claimId, Claim.DenialStatus claimDenialStatus, string commentText, bool deleteCharacter)
    {
        var claim = await LoadClaimForApprovalDecline(projectId, claimId, CurrentUserId);

        var statusWasApproved = claim.ClaimStatus == Claim.Status.Approved;

        claim.EnsureCanChangeStatus(Claim.Status.DeclinedByMaster);

        claim.MasterDeclinedDate = Now;
        claim.ClaimStatus = Claim.Status.DeclinedByMaster;
        claim.ClaimDenialStatus = claimDenialStatus;

        var roomEmail = await CommonClaimDecline(claim);

        if (deleteCharacter)
        {
            if (claim.Character is null || !statusWasApproved)
            {
                throw new InvalidOperationException("Attempt to delete character, but it not exists");
            }
            DeleteCharacter(claim.Character, CurrentUserId);
        }

        await _accommodationInviteService.DeclineAllClaimInvites(claimId).ConfigureAwait(false);

        var email =
          await
              AddCommentWithEmail<DeclineByMasterEmail>(commentText,
                  claim,
                  true,
                  s => s.ClaimStatusChange,
                  null,
                  CommentExtraAction.DeclineByMaster);

        await UnitOfWork.SaveChangesAsync();
        await EmailService.Email(email);
        if (roomEmail != null)
        {
            await EmailService.Email(roomEmail);
        }
    }

    private void DeleteCharacter(Character character, int currentUserId)
    {

        _ = character.RequestMasterAccess(currentUserId, acl => acl.CanEditRoles);
        MarkTreeModified(character.Project);

        character.DirectlyRelatedPlotElements.CleanLinksList();

        character.IsActive = false;
        MarkChanged(character);
    }

    private async Task<LeaveRoomEmail?> CommonClaimDecline(Claim claim)
    {
        MarkCharacterChangedIfApproved(claim);


        if (claim.Character?.ApprovedClaim == claim)
        {
            claim.Character.ApprovedClaimId = null;
        }

        return await ConsiderLeavingRoom(claim);
    }

    private async Task<LeaveRoomEmail?> ConsiderLeavingRoom(Claim claim)
    {
        LeaveRoomEmail? email = null;

        if (claim.AccommodationRequest != null)
        {
            if (claim.AccommodationRequest.Accommodation != null)
            {
                email = new LeaveRoomEmail()
                {
                    Changed = new[] { claim },
                    Initiator = await GetCurrentUser(),
                    ProjectName = claim.Project.ProjectName,
                    Recipients = claim.AccommodationRequest.Accommodation.GetSubscriptions().ToList(),
                    Room = claim.AccommodationRequest.Accommodation,
                    Text = new MarkdownString(),
                };
            }

            _ = claim.AccommodationRequest.Subjects.Remove(claim);
            if (!claim.AccommodationRequest.Subjects.Any())
            {
                _ = UnitOfWork.GetDbSet<AccommodationRequest>().Remove(claim.AccommodationRequest);
            }
        }

        return email;
    }

    /// <inheritdoc />
    public async Task<AccommodationRequest?> LeaveAccommodationGroupAsync(int projectId, int claimId)
    {
        var claim = await ClaimsRepository.GetClaim(projectId, claimId);

        claim = claim.RequestAccess(CurrentUserId,
            acl => acl.CanSetPlayersAccommodations,
            claim.ClaimStatus == Claim.Status.Approved
                ? ExtraAccessReason.PlayerOrResponsible
                : ExtraAccessReason.None);

        var acr = claim.AccommodationRequest;
        if (acr is null)
        {
            return null;
        }
        if (acr.Subjects.Count == 1)
        {
            return acr;
        }

        var email = EmailHelpers.CreateFieldsEmail(
            claim,
            s => s.AccommodationChange,
            await GetCurrentUser(),
            Array.Empty<FieldWithPreviousAndNewValue>(),
            new Dictionary<string, PreviousAndNewValue>()
            {
                {
                    "Тип поселения", //TODO[Localize]
                    new PreviousAndNewValue("без поселения", acr.AccommodationType.Name)
                },
            });
        var leaveEmail = await ConsiderLeavingRoom(claim);

        acr.Subjects.Remove(claim);
        claim.AccommodationRequest_Id = null;
        claim.AccommodationRequest = null;
        UnitOfWork.GetDbSet<AccommodationRequest>()
            .Add(
                new AccommodationRequest
                {
                    ProjectId = projectId,
                    AccommodationTypeId = acr.AccommodationTypeId,
                    IsAccepted = AccommodationRequest.InviteState.Accepted,
                    Subjects = new List<Claim> { claim }
                });

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(email);
        if (leaveEmail is not null)
        {
            await EmailService.Email(leaveEmail);
        }

        return acr;
    }

    public async Task<AccommodationRequest> SetAccommodationType(int projectId,
        int claimId,
        int roomTypeId)
    {
        //todo set first state to Unanswered
        var claim = await ClaimsRepository.GetClaim(projectId, claimId).ConfigureAwait(false);

        claim = claim.RequestAccess(CurrentUserId,
            acl => acl.CanSetPlayersAccommodations,
            claim?.ClaimStatus == Claim.Status.Approved
                ? ExtraAccessReason.PlayerOrResponsible
                : ExtraAccessReason.None);

        // Player cannot change accommodation type if already checked in

        if (claim.AccommodationRequest?.AccommodationTypeId == roomTypeId)
        {
            return claim.AccommodationRequest;
        }

        var newType = await UnitOfWork.GetDbSet<ProjectAccommodationType>().FindAsync(roomTypeId)
            .ConfigureAwait(false);

        if (newType == null)
        {
            throw new JoinRpgEntityNotFoundException(roomTypeId,
                nameof(ProjectAccommodationType));
        }

        var email = EmailHelpers.CreateFieldsEmail(claim,
            s => s.AccommodationChange,
            await GetCurrentUser(),
            Array.Empty<FieldWithPreviousAndNewValue>(),
            new Dictionary<string, PreviousAndNewValue>()
            {
              {
                  "Тип поселения", //TODO[Localize]
                  new PreviousAndNewValue(newType.Name, claim.AccommodationRequest?.AccommodationType.Name)
              },
            });


        var leaveEmail = await ConsiderLeavingRoom(claim);

        // TODO: Just change accommodation type if this claim is the only occupant of previous room
        var accommodationRequest = new AccommodationRequest
        {
            ProjectId = projectId,
            Subjects = new List<Claim> { claim },
            AccommodationTypeId = roomTypeId,
            IsAccepted = AccommodationRequest.InviteState.Accepted,
        };

        _ = UnitOfWork
            .GetDbSet<AccommodationRequest>()
            .Add(accommodationRequest);
        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        await EmailService.Email(email);
        if (leaveEmail != null)
        {
            await EmailService.Email(leaveEmail);
        }

        return accommodationRequest;
    }


    public async Task DeclineByPlayer(int projectId, int claimId, string commentText)
    {
        var claim = await ClaimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            throw new DbEntityValidationException();
        }

        _ = claim.RequestAccess(CurrentUserId, acl => false, ExtraAccessReason.Player);
        claim.EnsureCanChangeStatus(Claim.Status.DeclinedByUser);

        claim.PlayerDeclinedDate = Now;
        claim.ClaimStatus = Claim.Status.DeclinedByUser;


        await _accommodationInviteService.DeclineAllClaimInvites(claimId).ConfigureAwait(false);

        var roomEmail = await CommonClaimDecline(claim);



        var email =
            await
                AddCommentWithEmail<DeclineByPlayerEmail>(commentText,
                    claim,
                    true,
                    s => s.ClaimStatusChange,
                    null,
                    CommentExtraAction.DeclineByPlayer);

        await UnitOfWork.SaveChangesAsync();
        await EmailService.Email(email);
        if (roomEmail != null)
        {
            await EmailService.Email(roomEmail);
        }
    }


    public async Task RestoreByMaster(int projectId, int claimId, int currentUserId, string commentText)
    {
        var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);

        claim.EnsureCanChangeStatus(Claim.Status.AddedByUser);
        claim.ClaimStatus = Claim.Status.AddedByUser; //TODO: Actually should be "AddedByMaster" but we don't support it yet.
        claim.ClaimDenialStatus = null;
        SetDiscussed(claim, true);


        if (claim.Character != null)
        {
            if (claim.OtherClaimsForThisCharacter().Any(c => c.IsApproved))
            {
                claim.CharacterId = null;
                claim.CharacterGroupId = claim.Project.RootGroup.CharacterGroupId;
            }
            else
            {
                if (claim.Character != null)
                {
                    claim.Character.IsActive = true;
                    MarkChanged(claim.Character);
                }
            }
        }

        var email =
          await
            AddCommentWithEmail<RestoreByMasterEmail>(commentText, claim, true,
              s => s.ClaimStatusChange, null, CommentExtraAction.RestoreByMaster);

        await UnitOfWork.SaveChangesAsync();
        await EmailService.Email(email);
    }

    public async Task MoveByMaster(int projectId, int claimId, int currentUserId, string contents, int? characterGroupId, int? characterId)
    {
        var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
        var source = await ProjectRepository.GetClaimSource(projectId, characterGroupId, characterId);

        //Grab subscribtions before change
        var subscribe = claim.GetSubscriptions(s => s.ClaimStatusChange);

        source.EnsureCanMoveClaim(claim);

        MarkCharacterChangedIfApproved(claim); // before move

        if (claim.Character != null && claim.IsApproved)
        {
            claim.Character.ApprovedClaim = null;
        }
        claim.CharacterGroupId = characterGroupId;
        claim.CharacterId = characterId;
        claim.Group = source as CharacterGroup; //That fields is required later
        claim.Character = source as Character; //That fields is required later

        if (claim.Character != null && claim.IsApproved)
        {
            claim.Character.ApprovedClaim = claim;
        }

        MarkCharacterChangedIfApproved(claim); // after move

        if (claim.IsApproved && claim.CharacterId == null)
        {
            throw new DbEntityValidationException();
        }

        var email =
          await
            AddCommentWithEmail<MoveByMasterEmail>(contents, claim,
              isVisibleToPlayer: true, predicate: s => s.ClaimStatusChange, parentComment: null,
              extraAction: CommentExtraAction.MoveByMaster, extraSubscriptions: subscribe);

        await UnitOfWork.SaveChangesAsync();
        await EmailService.Email(email);
    }

    public async Task UpdateReadCommentWatermark(int projectId, int commentDiscussionId, int maxCommentId)
    {
        var watermarks =
          UnitOfWork.GetDbSet<ReadCommentWatermark>()
            .Where(w => w.CommentDiscussionId == commentDiscussionId && w.UserId == CurrentUserId)
            .OrderByDescending(wm => wm.ReadCommentWatermarkId)
            .ToList();

        //Sometimes watermarks can duplicate. If so, let's remove them.
        foreach (var wm in watermarks.Skip(1))
        {
            _ = UnitOfWork.GetDbSet<ReadCommentWatermark>().Remove(wm);
        }

        var watermark = watermarks.FirstOrDefault();

        if (watermark == null)
        {
            watermark = new ReadCommentWatermark()
            {
                CommentDiscussionId = commentDiscussionId,
                ProjectId = projectId,
                UserId = CurrentUserId,
            };
            _ = UnitOfWork.GetDbSet<ReadCommentWatermark>().Add(watermark);
        }

        if (watermark.CommentId > maxCommentId)
        {
            return;
        }
        watermark.CommentId = maxCommentId;
        await UnitOfWork.SaveChangesAsync();
    }

    private async Task<T> AddCommentWithEmail<T>(string commentText, Claim claim,
      bool isVisibleToPlayer, Func<UserSubscription, bool> predicate, Comment? parentComment,
      CommentExtraAction? extraAction = null, IEnumerable<User>? extraSubscriptions = null) where T : ClaimEmailModel, new()
    {
        var visibleToPlayerUpdated = isVisibleToPlayer && parentComment?.IsVisibleToPlayer != false;
        _ = AddCommentImpl(claim, parentComment, commentText, visibleToPlayerUpdated, extraAction);

        var extraRecipients =
          new[] { parentComment?.Author, parentComment?.Finance?.PaymentType?.User }.
          Union(extraSubscriptions ?? Enumerable.Empty<User>());

        var mastersOnly = !visibleToPlayerUpdated;
        return
          await CreateClaimEmail<T>(claim, commentText, predicate,
            extraAction, mastersOnly, extraRecipients);
    }

    public async Task SetResponsible(int projectId, int claimId, int currentUserId, int responsibleMasterId)
    {
        var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
        _ = claim.RequestMasterAccess(currentUserId);
        _ = claim.RequestMasterAccess(responsibleMasterId);

        if (responsibleMasterId == claim.ResponsibleMasterUserId)
        {
            return; // Just do nothing
        }

        var newMaster = await UserRepository.GetById(responsibleMasterId);

        var email = await
          AddCommentWithEmail<ChangeResponsibleMasterEmail>($"{claim.ResponsibleMasterUser.GetDisplayName() ?? "N/A"} → {newMaster.GetDisplayName()}", claim,
            isVisibleToPlayer: true, predicate: s => s.ClaimStatusChange, parentComment: null,
            extraAction: CommentExtraAction.ChangeResponsible, extraSubscriptions: new[] { newMaster });

        claim.ResponsibleMasterUserId = responsibleMasterId;

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(email);
    }

    [ItemNotNull]
    private async Task<Claim> LoadClaimForApprovalDecline(int projectId, int claimId, int currentUserId)
    {
        var claim = await ClaimsRepository.GetClaim(projectId, claimId);

        return claim.RequestAccess(currentUserId,
            acl => acl.CanManageClaims,
            ExtraAccessReason.ResponsibleMaster);
    }

    public async Task SaveFieldsFromClaim(int projectId,
        int characterId,
        IReadOnlyDictionary<int, string?> newFieldValue)
    {
        //TODO: Prevent lazy load here - use repository
        var claim = await LoadProjectSubEntityAsync<Claim>(projectId, characterId);

        var updatedFields = FieldSaveHelper.SaveCharacterFields(CurrentUserId, claim, newFieldValue,
          FieldDefaultValueGenerator);
        if (updatedFields.Any(f => f.Field.FieldBoundTo == FieldBoundTo.Character) && claim.Character != null)
        {
            MarkChanged(claim.Character);
        }
        var user = await GetCurrentUser();
        var email = EmailHelpers.CreateFieldsEmail(claim, s => s.FieldChange, user, updatedFields);

        await UnitOfWork.SaveChangesAsync();

        await EmailService.Email(email);
    }

    public async Task OnHoldByMaster(int projectId, int claimId, int currentUserId, string contents)
    {

        var claim = await LoadClaimForApprovalDecline(projectId, claimId, currentUserId);
        MarkCharacterChangedIfApproved(claim);
        claim.ChangeStatusWithCheck(Claim.Status.OnHold);


        var email =
          await
            AddCommentWithEmail<OnHoldByMasterEmail>(contents, claim, true,
              s => s.ClaimStatusChange, null, CommentExtraAction.OnHoldByMaster);

        await UnitOfWork.SaveChangesAsync();
        await EmailService.Email(email);
    }

    private void MarkCharacterChangedIfApproved(Claim claim)
    {
        if (claim.ClaimStatus == Claim.Status.Approved && claim.Character != null)
        {
            MarkChanged(claim.Character);
        }
    }

    private readonly IAccommodationInviteService _accommodationInviteService;
    public ClaimServiceImpl(IUnitOfWork unitOfWork, IEmailService emailService,
      IFieldDefaultValueGenerator fieldDefaultValueGenerator,
        IAccommodationInviteService accommodationInviteService,
        ICurrentUserAccessor currentUserAccessor
        ) : base(unitOfWork, emailService,
      fieldDefaultValueGenerator, currentUserAccessor) => _accommodationInviteService = accommodationInviteService;

    private void SetDiscussed(Claim claim, bool isVisibleToPlayer)
    {
        claim.LastUpdateDateTime = Now;
        if (claim.ClaimStatus == Claim.Status.AddedByMaster && CurrentUserId == claim.PlayerUserId)
        {
            claim.ClaimStatus = Claim.Status.Discussed;
        }

        if (claim.ClaimStatus == Claim.Status.AddedByUser && CurrentUserId != claim.PlayerUserId && isVisibleToPlayer)
        {
            claim.ClaimStatus = Claim.Status.Discussed;
        }
    }

    public async Task ConcealComment(int projectId,
        int commentId,
        int commentDiscussionId,
        int currentUserId)
    {
        var discussion = await ForumRepository.GetDiscussionByComment(projectId, commentId);
        var childComments = discussion.Comments.Where(c => c.ParentCommentId == commentId);
        var comment = discussion.Comments.FirstOrDefault(coment => coment.CommentId == commentId);

        if (comment == null)
        {
            throw new JoinRpgEntityNotFoundException(commentId, nameof(Comment));
        }

        if (comment.HasMasterAccess(currentUserId) && !childComments.Any() &&
            comment.IsVisibleToPlayer && !comment.IsCommentByPlayer)
        {
            comment.IsVisibleToPlayer = false;
            await UnitOfWork.SaveChangesAsync();
        }
        else
        {
            throw new JoinRpgConcealCommentException();
        }
    }
}

