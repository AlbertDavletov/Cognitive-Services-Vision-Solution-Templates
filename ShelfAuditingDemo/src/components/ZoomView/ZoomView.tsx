import React from 'react'
import { View } from 'react-native'
import { State, PinchGestureHandler, PinchGestureHandlerGestureEvent, PinchGestureHandlerStateChangeEvent } from 'react-native-gesture-handler'
import { styles } from './ZoomView.style'

interface ZoomViewProps {
    onPinchEnd: Function;
    onPinchStart: Function;
    onPinchProgress: Function;
}

export class ZoomView extends React.Component<ZoomViewProps, {}> {
    constructor(props: ZoomViewProps) {
        super(props);
    }

    render() {
        return (
            <PinchGestureHandler
                onGestureEvent={this.onGesturePinch}
                onHandlerStateChange={this.onPinchHandlerStateChange}>
                <View style={styles.preview}>
                    {this.props.children}
                </View>
            </PinchGestureHandler>
        );
    }

    onGesturePinch = (event: PinchGestureHandlerGestureEvent ) => {
        this.props.onPinchProgress(event.nativeEvent.scale)
    }

    onPinchHandlerStateChange = (event: PinchGestureHandlerStateChangeEvent) => {
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
}
