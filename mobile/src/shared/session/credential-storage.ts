import * as SecureStore from 'expo-secure-store';
import { CredentialStorage, SessionStorageKey, StorageFlag } from './credential-storage.types';

const options: SecureStore.SecureStoreOptions = { keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY };

export const credentialStorage: CredentialStorage = {
  native: true,
  readRefreshToken: () => SecureStore.getItemAsync(SessionStorageKey.RefreshToken, options),
  writeRefreshToken: value => SecureStore.setItemAsync(SessionStorageKey.RefreshToken, value, options),
  deleteRefreshToken: () => SecureStore.deleteItemAsync(SessionStorageKey.RefreshToken, options),
  isLogoutPending: async () => (await SecureStore.getItemAsync(SessionStorageKey.LogoutPending, options)) === StorageFlag.True,
  async setLogoutPending(pending) {
    if (pending) await SecureStore.setItemAsync(SessionStorageKey.LogoutPending, StorageFlag.True, options);
    else await SecureStore.deleteItemAsync(SessionStorageKey.LogoutPending, options);
  },
  exclusive: operation => operation(), // controller serializes native auth operations
};
