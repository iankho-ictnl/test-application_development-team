namespace PropertyAdmin.Api.V1;

// The enums of the V1 interface contract, sent as numbers; never renumber.
public enum AssetType { Building, Home }

public enum ActivityKind { Administration, Maintenance }

public enum PricingType { Hourly, Fixed }

public enum InvoiceStatus { Open, Paid }
