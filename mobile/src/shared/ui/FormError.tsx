import { Text } from "react-native";

import { formStyles } from "./FormScreen";

type Props = {
  message: string | null;
};

export function FormError({ message }: Props) {
  if (!message) {
    return null;
  }

  return (
    <Text accessibilityRole="alert" style={formStyles.error}>{message}</Text>
  );
}
