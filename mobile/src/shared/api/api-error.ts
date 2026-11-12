// This file contains the definition of the API error and problem details types.
// It enables consistent handling of API errors and problem details throughout the application.
export type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly code: string,
    public readonly problem?: ProblemDetails,
  ) {
    super(code);
    this.name = "ApiError";
  }
}
