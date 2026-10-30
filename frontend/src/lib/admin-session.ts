import { createContext, useContext } from 'react';
import type { Profile } from './types';
export const AdminUserContext = createContext<Profile>(null!);
export const useOwner = () => useContext(AdminUserContext).roles.includes('Owner');
