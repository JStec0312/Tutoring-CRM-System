import { NativeTabs } from 'expo-router/native-tabs';

import { NavigationLabel, RouteName } from '@/shared/config/navigation';

import { Colors } from '@/shared/theme/theme';

export default function AppTabs() {
  const colors = Colors.light;

  return (
    <NativeTabs
      backgroundColor={colors.background}
      indicatorColor={colors.backgroundElement}
      labelStyle={{ color: colors.text }}>
      <NativeTabs.Trigger name={RouteName.Home}>
        <NativeTabs.Trigger.Label>{NavigationLabel.Home}</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          src={require('@/assets/images/tabIcons/home.png')}
          renderingMode="template"
        />
      </NativeTabs.Trigger>

      <NativeTabs.Trigger name={RouteName.Explore}>
        <NativeTabs.Trigger.Label>{NavigationLabel.Explore}</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon
          src={require('@/assets/images/tabIcons/explore.png')}
          renderingMode="template"
        />
      </NativeTabs.Trigger>
    </NativeTabs>
  );
}
