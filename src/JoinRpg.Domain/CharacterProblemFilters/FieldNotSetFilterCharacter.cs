using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.ClaimProblemFilters;

namespace JoinRpg.Domain.CharacterProblemFilters;

internal class FieldNotSetFilterBase
{
    protected static IEnumerable<ClaimProblem> CheckFields(IEnumerable<FieldWithValue> fieldsToCheck, IClaimSource target) => fieldsToCheck.SelectMany(fieldWithValue => CheckField(target, fieldWithValue));

    private static IEnumerable<ClaimProblem> CheckField(IClaimSource target, FieldWithValue fieldWithValue)
    {
        if (!fieldWithValue.Field.CanHaveValue())
        {
            yield break;
        }

        var isAvailableForTarget = fieldWithValue.Field.IsAvailableForTarget(target);
        var hasValue = fieldWithValue.HasEditableValue;

        if (hasValue)
        {
            if (isAvailableForTarget)
            {
                yield break;
            }

            if (fieldWithValue.Field.IsActive)
            {
                yield return FieldProblem(ClaimProblemType.FieldShouldNotHaveValue, ProblemSeverity.Hint, fieldWithValue);
            }
        }
        else
        {
            if (!isAvailableForTarget)
            {
                yield break;
            }

            switch (fieldWithValue.Field.MandatoryStatus)
            {
                case MandatoryStatus.Optional:
                    break;
                case MandatoryStatus.Recommended:
                    yield return FieldProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Hint, fieldWithValue);
                    break;
                case MandatoryStatus.Required:
                    yield return FieldProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Warning, fieldWithValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [MustUseReturnValue]
    private static ClaimProblem FieldProblem(ClaimProblemType problemType, ProblemSeverity severity, FieldWithValue fieldWithValue) => new FieldRelatedProblem(problemType, severity, fieldWithValue.Field);
}

internal class FieldNotSetFilterCharacter : FieldNotSetFilterBase, IProblemFilter<Character>
{
    #region Implementation of IProblemFilter<in Character>

    public IEnumerable<ClaimProblem> GetProblems(Character character)
    {
        return
          CheckFields(
            character.GetFields()
              .Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Character || character.ApprovedClaim != null)
              .ToList(), character);
    }

    #endregion
}

internal class FieldNotSetFilterClaim : FieldNotSetFilterBase, IProblemFilter<Claim>
{
    #region Implementation of IProblemFilter<in Character>
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
        var projectFields = claim.GetFields();

        return CheckFields(projectFields.Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Claim || claim.IsApproved).ToList(), claim.GetTarget());
    }
    #endregion
}
