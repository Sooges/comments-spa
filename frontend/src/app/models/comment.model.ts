export interface Attachment {
  id: number;
  kind: 'image' | 'text';
  fileName: string;
  size: number;
  width: number | null;
  height: number | null;
}

export interface Comment {
  id: number;
  parentId: number | null;
  userName: string;
  email: string;
  homePage: string | null;
  // safe HTML that is made by the server
  text: string;
  createdAt: string;
  attachment: Attachment | null;
  replies: Comment[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Captcha {
  id: string;
  // a data URI that can be used as the src of an img
  image: string;
}

export type SortField = 'date' | 'userName' | 'email';
export type SortOrder = 'asc' | 'desc';
