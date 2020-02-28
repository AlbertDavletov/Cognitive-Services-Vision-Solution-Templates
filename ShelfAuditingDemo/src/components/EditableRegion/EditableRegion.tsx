import React from 'react'
import { View, Text, PanResponder, PanResponderInstance, GestureResponderEvent, PanResponderGestureState } from 'react-native'
import { styles, TouchableCircleSize, CircleButtonActiveColor, CircleButtonPressedColor } from './EditableRegion.style'
import { BoundingBox } from '../../models/ProductItem';

enum PointPosition {
    TopLeft,
    TopRight,
    BottomRight,
    BottomLeft
}

interface RegionProps {
    position: BoundingBox;
    data: any;
    positionChange: Function;
}

interface RegionState {
    left: number;
    top: number;
    width: number;
    height: number;
    topLeftPressed: boolean;
    topRightPressed: boolean;
    bottomRightPressed: boolean;
    bottomLeftPressed: boolean;
}

export class EditableRegion extends React.Component<RegionProps, RegionState> {
    private _originalLeft: number;
    private _originalTop: number;
    private _originalWidth: number;
    private _originalHeight: number;
    private topLeftPanResponder: PanResponderInstance;
    private topRightPanResponder: PanResponderInstance;
    private bottomRightPanResponder: PanResponderInstance;
    private bottomLeftPanResponder: PanResponderInstance;

    constructor(props: RegionProps) {
        super(props);
        const { position } = props;

        this._originalLeft = position.left;
        this._originalTop = position.top;
        this._originalWidth = position.width;
        this._originalHeight = position.height;

        this.state = {
            left: position.left,
            top: position.top,
            width: position.width,
            height: position.height,
            topLeftPressed: false,
            topRightPressed: false,
            bottomRightPressed: false,
            bottomLeftPressed: false
        };

        this.topLeftPanResponder = this.getPanResponder(PointPosition.TopLeft);
        this.topRightPanResponder = this.getPanResponder(PointPosition.TopRight);
        this.bottomRightPanResponder = this.getPanResponder(PointPosition.BottomRight);
        this.bottomLeftPanResponder = this.getPanResponder(PointPosition.BottomLeft);
    }

    render() {
        const { data } = this.props;
    
        let component = 
            <View style={{
                position: 'absolute',
                left: this.state.left - TouchableCircleSize / 2, 
                top: this.state.top - TouchableCircleSize / 2,
                width: this.state.width + TouchableCircleSize,
                height: this.state.height + TouchableCircleSize,
                padding: TouchableCircleSize / 2,
            }}>   
                <View style={styles.selectedRegion}>
                    <View style={styles.selectedRegionLabelPanel}>
                        <Text numberOfLines={1} style={styles.selectedRegionLabel}>{data.title}</Text>  
                    </View>
                </View>

                <View style={[styles.touchableCircleStyle, { left: 0, top: 0 }]} {...this.topLeftPanResponder.panHandlers}>
                    <View style={[styles.visibleCircleStyle, { backgroundColor: this.state.topLeftPressed ? CircleButtonPressedColor : CircleButtonActiveColor }]}/>
                </View>
                <View style={[styles.touchableCircleStyle, { right: 0, top: 0 }]} {...this.topRightPanResponder.panHandlers}>
                    <View style={[styles.visibleCircleStyle, { backgroundColor: this.state.topRightPressed ? CircleButtonPressedColor : CircleButtonActiveColor }]}/>
                </View>
                <View style={[styles.touchableCircleStyle, { right: 0, bottom: 0 }]} {...this.bottomRightPanResponder.panHandlers}>
                    <View style={[styles.visibleCircleStyle, { backgroundColor: this.state.bottomRightPressed ? CircleButtonPressedColor : CircleButtonActiveColor }]}/>
                </View>
                <View style={[styles.touchableCircleStyle, { left: 0, bottom: 0 }]} {...this.bottomLeftPanResponder.panHandlers}>
                    <View style={[styles.visibleCircleStyle, { backgroundColor: this.state.bottomLeftPressed ? CircleButtonPressedColor : CircleButtonActiveColor }]}/>
                </View>
            </View>;
        return component;
    }

    _handlePanResponderMove = (event: GestureResponderEvent, gestureState: PanResponderGestureState, pointPosition: PointPosition) => {
        let xSign, ySign;

        switch (pointPosition) {
            case PointPosition.TopLeft:
                xSign = gestureState.dx < 0 ? 1 : -1;
                ySign = gestureState.dy < 0 ? 1 : -1;

                this.setState({
                    left: this._originalLeft + gestureState.dx,
                    top: this._originalTop + gestureState.dy,
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;

            case PointPosition.TopRight:
                xSign = gestureState.dx > 0 ? 1 : -1;
                ySign = gestureState.dy < 0 ? 1 : -1;

                this.setState({
                    top: this._originalTop + gestureState.dy,
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;

            case PointPosition.BottomRight:
                xSign = gestureState.dx > 0 ? 1 : -1;
                ySign = gestureState.dy > 0 ? 1 : -1;
        
                this.setState({
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;

            case PointPosition.BottomLeft:
                xSign = gestureState.dx < 0 ? 1 : -1;
                ySign = gestureState.dy > 0 ? 1 : -1;
        
                this.setState({
                    left: this._originalLeft + gestureState.dx,
                    width: this._originalWidth + xSign * Math.abs(gestureState.dx),
                    height: this._originalHeight + ySign * Math.abs(gestureState.dy)
                });
                break;
        }
    };

    _handlePandResponderGrant = (event: GestureResponderEvent, gestureState: PanResponderGestureState, pointPosition: PointPosition) => {
        if (this.props.positionChange) {
            const position = {
                left: this.state.left,
                top: this.state.top,
                width: this.state.width,
                height: this.state.height,
            }
            this.props.positionChange(true, position);
        }

        switch (pointPosition) {
            case PointPosition.TopLeft:
                this.setState({ topLeftPressed: true });
                break;

            case PointPosition.TopRight:
                this.setState({ topRightPressed: true });
                break;

            case PointPosition.BottomRight:
                this.setState({ bottomRightPressed: true });
                break;

            case PointPosition.BottomLeft:
                this.setState({ bottomLeftPressed: true });
                break;
        }
    }

    _handlePanResponderEnd = (event: GestureResponderEvent, gestureState: PanResponderGestureState, pointPosition: PointPosition) => {
        let xSign = 1;
        let ySign = 1;

        if (this.props.positionChange) {
            const position = {
                left: this.state.left,
                top: this.state.top,
                width: this.state.width,
                height: this.state.height,
            }
            this.props.positionChange(false, position);
        }

        switch (pointPosition) {
            case PointPosition.TopLeft:
                this.setState({ topLeftPressed: false });
                xSign = -1;
                ySign = -1;
                
                this._originalLeft += gestureState.dx;
                this._originalTop += gestureState.dy;
                break;

            case PointPosition.TopRight:
                this.setState({ topRightPressed: false });
                xSign = +1;
                ySign = -1;

                this._originalTop += gestureState.dy;
                break;

            case PointPosition.BottomRight:
                this.setState({ bottomRightPressed: false });
                xSign = +1;
                ySign = +1;
                break;

            case PointPosition.BottomLeft:
                this.setState({ bottomLeftPressed: false });
                xSign = -1;
                ySign = +1;

                this._originalLeft += gestureState.dx;
                break;
        }

        this._originalWidth += xSign * gestureState.dx;
        this._originalHeight += ySign * gestureState.dy;
    };

    getPanResponder(pointPosition: PointPosition) {
        return PanResponder.create({
            onStartShouldSetPanResponder: (event, gestureState) => { return true; },
            onMoveShouldSetPanResponder: (event, gestureState) => { return true; },
            onPanResponderGrant: (event, gestureState) => this._handlePandResponderGrant(event, gestureState, pointPosition),
            onPanResponderMove: (event, gestureState) => this._handlePanResponderMove(event, gestureState, pointPosition),
            onPanResponderRelease: (event, gestureState) => this._handlePanResponderEnd(event, gestureState, pointPosition),
            onPanResponderTerminate: (event, gestureState) => this._handlePanResponderEnd(event, gestureState, pointPosition)
        });
    }
}
