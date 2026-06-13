import { request } from './http';
import type { ProviderInfo } from '../types/providers';

export const getProviders = () => request<ProviderInfo[]>('/api/providers');
