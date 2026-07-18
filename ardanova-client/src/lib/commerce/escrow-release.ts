export type TransactionReferenceValidation =
  | { isValid: true; value: string | undefined; error: null }
  | { isValid: false; value: undefined; error: string };

const opaqueTransactionReferencePattern = /^[A-Za-z0-9:_-]{12,200}$/;

/** Validates the backend's optional opaque transaction-reference field. */
export function validateTransactionReference(
  input: string,
): TransactionReferenceValidation {
  const value = input.trim();
  if (!value) return { isValid: true, value: undefined, error: null };

  if (!opaqueTransactionReferencePattern.test(value)) {
    return {
      isValid: false,
      value: undefined,
      error:
        "Use 12-200 letters, numbers, colons, hyphens, or underscores with no spaces, or leave this optional field blank.",
    };
  }

  return { isValid: true, value, error: null };
}
