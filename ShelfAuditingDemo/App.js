import React, { Component } from 'react';
import { Text, TouchableOpacity, Alert } from 'react-native';
import { createAppContainer } from 'react-navigation';
import { createStackNavigator } from 'react-navigation-stack';
import { InputScreen, ReviewScreen, ResultScreen, CameraScreen, TestScreen } from './src/views';
import Icon from 'react-native-vector-icons/EvilIcons';
Icon.loadFont();

const MainNavigator = createStackNavigator({
  Input: {
    screen: InputScreen,
    navigationOptions: { title: 'Shelf Audit',
      headerRight: () => (
        <Icon.Button name="gear" size={25}
          backgroundColor="transparent" 
          underlayColor="transparent"
          disabled={true}
          color="gray"
          onPress={() => console.log('This is a settings!')} />
    )}
  },
  Review: {
    screen: ReviewScreen,
    navigationOptions: { title: 'Review',
    headerRight: () => (
      <TouchableOpacity activeOpacity={0.6} onPress={() => Alert.alert('Publish!')} disabled={true}
        style={{ padding: 4, marginRight: 10 }}>
        <Text style={{color: 'gray', fontSize: 16, fontWeight: 'bold' }}>Publish</Text>
      </TouchableOpacity>)
    }
  },
  Result: {
    screen: ResultScreen,
    navigationOptions: { title: 'Detection results' }
  },
  Test: {
    screen: TestScreen,
    navigationOptions: { title: 'Test page' }
  },
  Camera: {
    screen: CameraScreen,
    navigationOptions: { title: 'Camera' }
  }},
  {
    initialRouteName: 'Input',
    defaultNavigationOptions: {
      headerStyle: {
        backgroundColor: '#1F1F1F',
      },
      headerTintColor: '#fff',
      headerTitleStyle: {
        fontWeight: 'bold',
      },
    },
  }
);

const App = createAppContainer(MainNavigator);

export default App;
