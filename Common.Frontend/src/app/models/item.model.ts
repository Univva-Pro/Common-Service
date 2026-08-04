export interface CommonItemResponse {
  itemId: string;
  originalId?: string;
  sourceService?: string;
  name: string;
  category: string;
  price: number;
  stockQuantity?: number;
}

export interface CommonItemAdminResponse extends CommonItemResponse {
  stockQuantity: number;
}

export interface CommonItemRequest {
  name: string;
  category: string;
  price: number;
  stockQuantity: number;
  sourceService?: string;
}
