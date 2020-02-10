import React, { Component } from 'react';
import { View, Text, ScrollView, Image, Dimensions, PanResponder, Animated, StyleSheet } from 'react-native';
import { PinchGestureHandler, State } from 'react-native-gesture-handler'
import { width, height } from '../../constants';

class TestScreen extends React.Component {
    constructor(props) {
        super(props);

        this.state = {
            scale: 1
        };
    }

    componentDidMount() {
        const { navigation } = this.props;
    }

    render() {
        const { mainContainer } = this.styles;

        return (
            <View style={{ flex: 1 }}>
                    <Image style={{ flex: 1, width: null, height: '100%' }}
                        source={{ uri: 'https://miro.medium.com/max/1080/1*7SYuZvH2pZnM0H79V4ttPg.jpeg' }}
                        resizeMode="contain" 
                        transform={[{ scale: this.state.scale}]}/>
            </View>
        );
    }

    onZoomEvent = Animated.event(
        [
            {
                nativeEvent: { scale: this.scale }
            }
        ],
        {
            useNativeDriver: true
        }
    )

    onZoomStateChange = event => {
        if (event.nativeEvent.oldState === State.ACTIVE) {
            Animated.spring(this.scale, {
                toValue: 1,
                useNativeDriver: true
            }).start()
        }
    }

    styles = StyleSheet.create({
        mainContainer: {
            flex: 1,
        },
    })
}

export { TestScreen };