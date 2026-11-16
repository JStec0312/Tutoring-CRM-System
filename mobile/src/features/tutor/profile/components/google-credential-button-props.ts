export type GoogleCredentialButtonProps = {
  nonce: string;
  locale?: string;
  onCredential: (nonce: string, idToken: string) => void;
  onFailure: (nonce: string) => void;
  onCancel: (nonce: string) => void;
};
