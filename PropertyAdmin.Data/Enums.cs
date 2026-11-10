namespace PropertyAdmin.Data;

// Stored values: PricingType is stored by name, the others by number; never rename or renumber.
public enum AssetType { Building, Home }

public enum ActivityKind { Administration, Maintenance }

public enum PricingType { Hourly, Fixed }

public enum InvoiceStatus { Open, Paid }
