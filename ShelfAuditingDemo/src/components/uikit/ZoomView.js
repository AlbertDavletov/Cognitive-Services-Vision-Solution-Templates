import React, { Component } from 'react';
import { View, StyleSheet } from 'react-native';
import { PinchGestureHandler, State } from 'react-native-gesture-handler';
import { width, height } from '../../../constants'

class ZoomView extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
        };
    }

    render() {
        const { preview } = this.styles;

        return (
            <PinchGestureHandler
                onGestureEvent={this.onGesturePinch}
                onHandlerStateChange={this.onPinchHandlerStateChange}>
                <View style={preview}>
                    {this.props.children}
                </View>
            </PinchGestureHandler>
        );
    }

    onGesturePinch = ({ nativeEvent }) => {
        this.props.onPinchProgress(nativeEvent.scale)
    }

    onPinchHandlerStateChange = (event) => {
        const pinch_end = event.nativeEvent.state === State.END
        const pinch_begin = event.nativeEvent.oldState === State.BEGAN
        const pinch_active = event.nativeEvent.state === State.ACTIVE
        if (pinch_end) {
          this.props.onPinchEnd()
        }
        else if (pinch_begin && pinch_active) {
          this.props.onPinchStart()
        }
    }    

    styles = StyleSheet.create({
        preview: {
            height: height,
            width: "100%",
        },
    })
}

export { ZoomView };