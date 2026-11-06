import { create } from 'zustand'

// Deliberately not persisted: neither tokens nor private profiles go into browser storage.
export const useAuthStore = create((set) => ({
  access: null,
  status: 'loading',
  recoveryError: null,
  setAccess: (access) => set({ access, status: 'authenticated', recoveryError: null }),
  clear: () => set({ access: null, status: 'anonymous', recoveryError: null }),
  unavailable: (error) => set({ status: 'unavailable', recoveryError: error }),
}))
