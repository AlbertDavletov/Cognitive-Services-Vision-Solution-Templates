import React, { Component } from 'react';
import { 
    View, 
    Text, 
    Image, 
    Animated, 
    TouchableOpacity,
    StyleSheet, 
    Alert 
} from 'react-native';
import { PinchGestureHandler, State } from 'react-native-gesture-handler'
import { width, height } from '../../constants';

class TestScreen extends React.Component {
    static navigationOptions = ({ navigation }) => {
        const { params } = navigation.state;
        return { 
            title: 'Test page',
            headerRight: () => (
                <TouchableOpacity activeOpacity={0.6} onPress={() => params.testApply()}
                    style={{ padding: 4, marginRight: 10 }}>
                    <Text style={{color: 'white', fontSize: 16, fontWeight: 'bold' }}>Apply</Text>
                </TouchableOpacity>
            )
        }
    }

    constructor(props) {
        super(props);

        this.state = {
            scale: 1
        };
    }

    componentDidMount() {
        const { navigation } = this.props;
        navigation.setParams({ testApply: this.testApply });
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

    testApply() {
        Alert.alert('testApply!')
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