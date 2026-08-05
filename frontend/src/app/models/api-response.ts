export enum ResultStatus {
  Ok = 'Ok',
  Created = 'Created',
  NotFound = 'NotFound',
  Unauthorized = 'Unauthorized',
  Forbidden = 'Forbidden',
  Conflict = 'Conflict',
  InternalError = 'InternalError',
}

export interface StandardResponse<T> {
  success: boolean;
  data: T | null;
  status: ResultStatus;
  message: string | null;
  errors: string[] | null;
}

export interface PaginatedResponse<T> extends StandardResponse<T[]> {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
