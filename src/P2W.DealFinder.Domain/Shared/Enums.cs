namespace P2W.DealFinder.Domain.Shared;

public enum ProductCondition
{
    Unknown = 0,
    NearMint = 1,
    LightlyPlayed = 2,
    ModeratelyPlayed = 3,
    HeavilyPlayed = 4,
    Damaged = 5,
    New = 6,
    Used = 7
}

public enum IdentityMatchStatus
{
    Unknown = 0,
    AutoMatched = 1,
    NeedsReview = 2,
    Approved = 3,
    Rejected = 4
}

public enum ObservationStatus
{
    Started = 0,
    Succeeded = 1,
    NoData = 2,
    Failed = 3,
    RateLimited = 4,
    Disabled = 5
}

public enum MarketValueBasis
{
    Unknown = 0,
    SoldCompMedian = 1,
    SoldCompAverage = 2,
    ReferencePrice = 3,
    ActiveListingMedian = 4,
    ManualOverride = 5
}

public enum DealCandidateStatus
{
    New = 0,
    Watch = 1,
    Actionable = 2,
    Rejected = 3,
    Purchased = 4,
    Ignored = 5
}

public enum DealDecision
{
    None = 0,
    Watch = 1,
    Buy = 2,
    Reject = 3,
    NeedsMoreEvidence = 4
}
